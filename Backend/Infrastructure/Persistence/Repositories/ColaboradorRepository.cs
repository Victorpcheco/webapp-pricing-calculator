// Infrastructure/Persistence/Repositories/ColaboradorRepository.cs
using Application.Colaboradores.Services;
using Application.Repositories;
using Domain.Common;
using Domain.Entities.Colaboradores;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ColaboradorRepository : IColaboradorRepository, IScopedService
{
    private readonly AppDbContext _context;

    public ColaboradorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Colaborador>> ListarPorUsuarioAsync(
        Guid usuarioId,
        string? busca,
        TipoContratacao? tipo,
        CancellationToken ct = default)
    {
        var consulta = _context.Colaboradores
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            // ILike = busca parcial sem distinção de maiúsculas (Postgres).
            // O campo único da toolbar procura em nome E cargo.
            var termo = $"%{EscaparCuringas(busca.Trim())}%";
            consulta = consulta.Where(c =>
                EF.Functions.ILike(c.Nome, termo, @"\") ||
                EF.Functions.ILike(c.Cargo, termo, @"\"));
        }

        if (tipo.HasValue)
            consulta = consulta.Where(c => c.TipoContratacao == tipo.Value);

        // Mais recentes primeiro — o frontend adiciona novos itens no topo da lista
        return await consulta
            .OrderByDescending(c => c.CriadoEm)
            .ToListAsync(ct);
    }

    public async Task<ColaboradoresResumo> ObterResumoAsync(Guid usuarioId, CancellationToken ct = default)
    {
        // Uma única ida ao banco para alimentar os quatro cards de estatística
        var resumo = await _context.Colaboradores
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Clt = g.Count(c => c.TipoContratacao == TipoContratacao.Clt),
                Freelancers = g.Count(c => c.TipoContratacao == TipoContratacao.Freelancer),
                CustoEquipe = g.Sum(c => c.CustoMensal)
            })
            .SingleOrDefaultAsync(ct);

        return resumo is null
            ? ColaboradoresResumo.Vazio
            : new ColaboradoresResumo(resumo.Total, resumo.Clt, resumo.Freelancers, resumo.CustoEquipe);
    }

    public async Task<Colaborador?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct = default)
    {
        return await _context.Colaboradores
            .SingleOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId, ct);
    }

    public async Task AdicionarAsync(Colaborador colaborador, CancellationToken ct = default)
    {
        await _context.Colaboradores.AddAsync(colaborador, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(Colaborador colaborador, CancellationToken ct = default)
    {
        _context.Colaboradores.Update(colaborador);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoverAsync(Colaborador colaborador, CancellationToken ct = default)
    {
        _context.Colaboradores.Remove(colaborador);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoverTodosPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        await _context.Colaboradores
            .Where(c => c.UsuarioId == usuarioId)
            .ExecuteDeleteAsync(ct);
    }

    /// <summary>Impede que %, _ ou \ digitados na busca sejam interpretados como curingas.</summary>
    private static string EscaparCuringas(string termo) => termo
        .Replace(@"\", @"\\")
        .Replace("%", @"\%")
        .Replace("_", @"\_");
}
