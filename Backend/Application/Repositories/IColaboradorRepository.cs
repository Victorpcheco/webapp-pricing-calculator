// Application/Repositories/IColaboradorRepository.cs
using Application.Colaboradores.Services;
using Domain.Entities.Colaboradores;

namespace Application.Repositories;

public interface IColaboradorRepository
{
    /// <summary>A busca é aplicada a nome OU cargo, como o campo único da toolbar.</summary>
    Task<IReadOnlyList<Colaborador>> ListarPorUsuarioAsync(
        Guid usuarioId,
        string? busca,
        TipoContratacao? tipo,
        CancellationToken ct = default);

    /// <summary>Totais globais do usuário, independentes dos filtros da listagem.</summary>
    Task<ColaboradoresResumo> ObterResumoAsync(Guid usuarioId, CancellationToken ct = default);

    Task<Colaborador?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct = default);
    Task AdicionarAsync(Colaborador colaborador, CancellationToken ct = default);
    Task AtualizarAsync(Colaborador colaborador, CancellationToken ct = default);
    Task RemoverAsync(Colaborador colaborador, CancellationToken ct = default);
    Task RemoverTodosPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
}
