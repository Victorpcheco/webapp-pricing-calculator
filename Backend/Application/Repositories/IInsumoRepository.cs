// Application/Repositories/IInsumoRepository.cs
using Application.Insumos.Services;
using Domain.Entities.Insumos;

namespace Application.Repositories;

public interface IInsumoRepository
{
    Task<IReadOnlyList<Insumo>> ListarPorUsuarioAsync(
        Guid usuarioId,
        string? nome,
        TipoInsumo? tipo,
        CancellationToken ct = default);

    /// <summary>Totais globais do usuário, independentes dos filtros da listagem.</summary>
    Task<InsumosResumo> ObterResumoAsync(Guid usuarioId, CancellationToken ct = default);

    Task<Insumo?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct = default);

    /// <summary>Resolve vários insumos de uma vez — usado pela composição da ficha técnica.</summary>
    Task<IReadOnlyList<Insumo>> ListarPorIdsAsync(
        Guid usuarioId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);
    Task AdicionarAsync(Insumo insumo, CancellationToken ct = default);
    Task AtualizarAsync(Insumo insumo, CancellationToken ct = default);
    Task RemoverAsync(Insumo insumo, CancellationToken ct = default);
    Task RemoverTodosPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
}
