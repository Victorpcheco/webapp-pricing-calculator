// Application/Insumos/Services/InsumosListResult.cs
namespace Application.Insumos.Services;

/// <summary>Envelope do GET /api/insumos: a lista filtrada + os totais globais.</summary>
public record InsumosListResult(
    IReadOnlyList<InsumoResult> Data,
    InsumosResumo Meta
);
