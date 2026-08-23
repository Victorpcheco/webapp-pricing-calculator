// Application/Produtos/Services/ProdutoResult.cs
namespace Application.Produtos.Services;

/// <summary>Uma linha da composição, com o insumo já resolvido.</summary>
public record ItemComposicaoResult(
    Guid SupplyId,
    string? SupplyName,
    /// <summary>false quando o insumo foi excluído depois da ficha criada.</summary>
    bool SupplyAvailable,
    decimal Amount,
    string BaseUnit,
    decimal SupplyUnitCost,
    decimal Cost
);

/// <summary>Ficha técnica com os custos recalculados no momento da leitura.</summary>
public record ProdutoResult(
    Guid Id,
    string Name,
    string ProductionType,
    int YieldAmount,
    string YieldName,
    int ProductionTime,
    IReadOnlyList<ItemComposicaoResult> Composition,
    decimal MaterialsCost,
    decimal LaborCost,
    decimal TotalCost,
    decimal UnitCost,
    decimal HourlyRateUsed,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
