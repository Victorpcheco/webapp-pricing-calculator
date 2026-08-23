// Application/Repositories/IPrecificacaoRepository.cs
using Domain.Entities.Precificacoes;

namespace Application.Repositories;

public interface IPrecificacaoRepository
{
    Task<IReadOnlyList<SimulacaoPreco>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
    Task<SimulacaoPreco?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct = default);
    Task AdicionarAsync(SimulacaoPreco simulacao, CancellationToken ct = default);
    Task AtualizarAsync(SimulacaoPreco simulacao, CancellationToken ct = default);
    Task RemoverAsync(SimulacaoPreco simulacao, CancellationToken ct = default);
    Task RemoverTodosPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
}
