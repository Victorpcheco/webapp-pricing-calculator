// Infrastructure/Persistence/Repositories/InsumoRepository.cs
using Application.Insumos.Services;
using Application.Repositories;
using Domain.Common;
using Domain.Entities.Insumos;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class InsumoRepository : IInsumoRepository, IScopedService
{
    private readonly AppDbContext _context;

    public InsumoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Insumo>> ListarPorUsuarioAsync(
        Guid usuarioId,
        string? nome,
        TipoInsumo? tipo,
        CancellationToken ct = default)
    {
        var consulta = _context.Insumos
            .AsNoTracking()
            .Where(i => i.UsuarioId == usuarioId);

        if (!string.IsNullOrWhiteSpace(nome))
        {
            // ILike = busca parcial sem distinção de maiúsculas (Postgres)
            var termo = $"%{EscaparCuringas(nome.Trim())}%";
            consulta = consulta.Where(i => EF.Functions.ILike(i.Nome, termo, @"\"));
        }

        if (tipo.HasValue)
            consulta = consulta.Where(i => i.Tipo == tipo.Value);

        // Mais recentes primeiro — o frontend adiciona novos itens no topo da lista
        return await consulta
            .OrderByDescending(i => i.CriadoEm)
            .ToListAsync(ct);
    }

    public async Task<InsumosResumo> ObterResumoAsync(Guid usuarioId, CancellationToken ct = default)
    {
        // Uma única ida ao banco para alimentar os quatro cards de estatística
        var resumo = await _context.Insumos
            .AsNoTracking()
            .Where(i => i.UsuarioId == usuarioId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Ingredientes = g.Count(i => i.Tipo == TipoInsumo.Ingrediente),
                Embalagens = g.Count(i => i.Tipo == TipoInsumo.Embalagem),
                ValorCompras = g.Sum(i => i.Preco)
            })
            .SingleOrDefaultAsync(ct);

        return resumo is null
            ? InsumosResumo.Vazio
            : new InsumosResumo(resumo.Total, resumo.Ingredientes, resumo.Embalagens, resumo.ValorCompras);
    }

    public async Task<Insumo?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct = default)
    {
        return await _context.Insumos
            .SingleOrDefaultAsync(i => i.Id == id && i.UsuarioId == usuarioId, ct);
    }

    public async Task<IReadOnlyList<Insumo>> ListarPorIdsAsync(
        Guid usuarioId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return Array.Empty<Insumo>();

        return await _context.Insumos
            .AsNoTracking()
            .Where(i => i.UsuarioId == usuarioId && ids.Contains(i.Id))
            .ToListAsync(ct);
    }

    public async Task AdicionarAsync(Insumo insumo, CancellationToken ct = default)
    {
        await _context.Insumos.AddAsync(insumo, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(Insumo insumo, CancellationToken ct = default)
    {
        _context.Insumos.Update(insumo);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoverAsync(Insumo insumo, CancellationToken ct = default)
    {
        _context.Insumos.Remove(insumo);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoverTodosPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        await _context.Insumos
            .Where(i => i.UsuarioId == usuarioId)
            .ExecuteDeleteAsync(ct);
    }

    /// <summary>Impede que %, _ ou \ digitados na busca sejam interpretados como curingas.</summary>
    private static string EscaparCuringas(string termo) => termo
        .Replace(@"\", @"\\")
        .Replace("%", @"\%")
        .Replace("_", @"\_");
}
