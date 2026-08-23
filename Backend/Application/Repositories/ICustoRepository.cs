// Application/Repositories/ICustoRepository.cs
using Domain.Entities.Custos;

namespace Application.Repositories;

public interface ICustoRepository
{
    Task<IReadOnlyList<CustoOperacional>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
    Task<CustoOperacional?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct = default);
    Task AdicionarAsync(CustoOperacional custo, CancellationToken ct = default);
    Task AtualizarAsync(CustoOperacional custo, CancellationToken ct = default);
    Task RemoverAsync(CustoOperacional custo, CancellationToken ct = default);
    Task RemoverTodosPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
}
