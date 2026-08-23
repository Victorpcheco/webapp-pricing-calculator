// Application/Colaboradores/Services/ColaboradoresListResult.cs
namespace Application.Colaboradores.Services;

/// <summary>Envelope do GET /api/colaboradores: a lista filtrada + os totais globais.</summary>
public record ColaboradoresListResult(
    IReadOnlyList<ColaboradorResult> Data,
    ColaboradoresResumo Meta
);
