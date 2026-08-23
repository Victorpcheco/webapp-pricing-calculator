// Infrastructure/Persistence/Repositories/PrecificacaoRepository.cs
using Application.Repositories;
using Domain.Common;
using Domain.Entities.Precificacoes;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class PrecificacaoRepository : IPrecificacaoRepository, IScopedService
{
    private readonly AppDbContext _context;

    public PrecificacaoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SimulacaoPreco>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        // Mais recentes primeiro — mesma ordem do histórico de custos operacionais
        return await _context.SimulacoesPreco
            .AsNoTracking()
            .Where(s => s.UsuarioId == usuarioId)
            .OrderByDescending(s => s.CriadoEm)
            .ToListAsync(ct);
    }

    public async Task<SimulacaoPreco?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct = default)
    {
        return await _context.SimulacoesPreco
            .SingleOrDefaultAsync(s => s.Id == id && s.UsuarioId == usuarioId, ct);
    }

    public async Task AdicionarAsync(SimulacaoPreco simulacao, CancellationToken ct = default)
    {
        await _context.SimulacoesPreco.AddAsync(simulacao, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(SimulacaoPreco simulacao, CancellationToken ct = default)
    {
        _context.SimulacoesPreco.Update(simulacao);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoverAsync(SimulacaoPreco simulacao, CancellationToken ct = default)
    {
        _context.SimulacoesPreco.Remove(simulacao);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoverTodosPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        await _context.SimulacoesPreco
            .Where(s => s.UsuarioId == usuarioId)
            .ExecuteDeleteAsync(ct);
    }
}
