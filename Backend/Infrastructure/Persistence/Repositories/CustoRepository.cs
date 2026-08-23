// Infrastructure/Persistence/Repositories/CustoRepository.cs
using Application.Repositories;
using Domain.Common;
using Domain.Entities.Custos;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CustoRepository : ICustoRepository, IScopedService
{
    private readonly AppDbContext _context;

    public CustoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CustoOperacional>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        return await _context.CustosOperacionais
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.CriadoEm)
            .ToListAsync(ct);
    }

    public async Task<CustoOperacional?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct = default)
    {
        return await _context.CustosOperacionais
            .SingleOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId, ct);
    }

    public async Task<decimal> ObterValorHoraAtualAsync(Guid usuarioId, CancellationToken ct = default)
    {
        // Configuração mais recente do usuário; 0 quando ainda não há nenhuma salva
        return await _context.CustosOperacionais
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.CriadoEm)
            .Select(c => c.ValorHora)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AdicionarAsync(CustoOperacional custo, CancellationToken ct = default)
    {
        await _context.CustosOperacionais.AddAsync(custo, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(CustoOperacional custo, CancellationToken ct = default)
    {
        _context.CustosOperacionais.Update(custo);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoverAsync(CustoOperacional custo, CancellationToken ct = default)
    {
        _context.CustosOperacionais.Remove(custo);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoverTodosPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        await _context.CustosOperacionais
            .Where(c => c.UsuarioId == usuarioId)
            .ExecuteDeleteAsync(ct);
    }
}
