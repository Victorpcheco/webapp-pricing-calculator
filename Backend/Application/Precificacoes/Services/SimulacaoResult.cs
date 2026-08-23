// Application/Precificacoes/Services/SimulacaoResult.cs
namespace Application.Precificacoes.Services;

/// <summary>Uma simulação de preço salva, com todos os valores derivados já calculados.</summary>
public record SimulacaoResult(
    Guid Id,
    Guid RecipeId,
    string RecipeName,
    decimal Cost,
    decimal Margin,
    decimal Suggested,
    decimal SalePrice,
    int Quantity,
    decimal Profit,
    decimal RealMargin,
    decimal Revenue,
    decimal TotalProfit,
    DateTime CreatedAt
);
