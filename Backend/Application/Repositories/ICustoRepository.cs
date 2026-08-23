// Application/Repositories/ICustoRepository.cs
using Domain.Entities.Custos;

namespace Application.Repositories;

public interface ICustoRepository
{
    Task<IReadOnlyList<CustoOperacional>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
    Task<CustoOperacional?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct = default);

    /// <summary>Valor da hora da configuração mais recente; 0 se o usuário ainda não salvou nenhuma.</summary>
    Task<decimal> ObterValorHoraAtualAsync(Guid usuarioId, CancellationToken ct = default);
    Task AdicionarAsync(CustoOperacional custo, CancellationToken ct = default);
    Task AtualizarAsync(CustoOperacional custo, CancellationToken ct = default);
    Task RemoverAsync(CustoOperacional custo, CancellationToken ct = default);
    Task RemoverTodosPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
}
