// Infrastructure/Persistence/Repositories/ProdutoRepository.cs
using Application.Repositories;
using Domain.Common;
using Domain.Entities.Produtos;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ProdutoRepository : IProdutoRepository, IScopedService
{
    private readonly AppDbContext _context;

    public ProdutoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Produto>> ListarPorUsuarioAsync(
        Guid usuarioId,
        string? nome,
        CancellationToken ct = default)
    {
        var consulta = _context.Produtos
            .AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId);

        if (!string.IsNullOrWhiteSpace(nome))
        {
            var termo = $"%{EscaparCuringas(nome.Trim())}%";
            consulta = consulta.Where(p => EF.Functions.ILike(p.Nome, termo, @"\"));
        }

        // Mais recentes primeiro — o frontend adiciona novos itens no topo
        return await consulta
            .OrderByDescending(p => p.AtualizadoEm)
            .ToListAsync(ct);
    }

    public async Task<Produto?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct = default)
    {
        return await _context.Produtos
            .SingleOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuarioId, ct);
    }

    public async Task AdicionarAsync(Produto produto, CancellationToken ct = default)
    {
        await _context.Produtos.AddAsync(produto, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(Produto produto, CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoverAsync(Produto produto, CancellationToken ct = default)
    {
        _context.Produtos.Remove(produto);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoverTodosPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        // ExecuteDelete não cascateia para tabelas owned; carregar garante a remoção das filhas
        var produtos = await _context.Produtos
            .Where(p => p.UsuarioId == usuarioId)
            .ToListAsync(ct);

        if (produtos.Count == 0) return;

        _context.Produtos.RemoveRange(produtos);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>Impede que %, _ ou \ digitados na busca sejam interpretados como curingas.</summary>
    private static string EscaparCuringas(string termo) => termo
        .Replace(@"\", @"\\")
        .Replace("%", @"\%")
        .Replace("_", @"\_");
}
