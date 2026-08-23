// Application/Insumos/Services/InsumoResult.cs
namespace Application.Insumos.Services;

/// <summary>Um item da tabela de insumos, já com os campos calculados pelo backend.</summary>
public record InsumoResult(
    Guid Id,
    string Name,
    string Type,
    decimal Quantity,
    string Unit,
    decimal Price,
    decimal UnitCost,
    decimal BaseQuantity,
    string BaseUnit,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
