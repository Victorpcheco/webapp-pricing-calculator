// Application/Repositories/IProdutoRepository.cs
using Domain.Entities.Produtos;

namespace Application.Repositories;

public interface IProdutoRepository
{
    Task<IReadOnlyList<Produto>> ListarPorUsuarioAsync(
        Guid usuarioId,
        string? nome,
        CancellationToken ct = default);

    Task<Produto?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct = default);
    Task AdicionarAsync(Produto produto, CancellationToken ct = default);
    Task AtualizarAsync(Produto produto, CancellationToken ct = default);
    Task RemoverAsync(Produto produto, CancellationToken ct = default);
    Task RemoverTodosPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
}
