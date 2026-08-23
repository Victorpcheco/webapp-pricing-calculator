// Application/Resultados/Services/ResultadoResult.cs
namespace Application.Resultados.Services;

/// <summary>
/// Uma linha da tabela de desempenho. Quando Priced é false, o produto ainda não
/// tem nenhuma simulação salva no período — SalePrice/Profit/Margin vêm nulos e a
/// tela exibe "—", como no mockup.
/// </summary>
public record ResultadoRow(
    Guid ProductId,
    string Name,
    string Unit,
    decimal Cost,
    decimal? SalePrice,
    decimal? Profit,
    decimal? Margin,
    bool Priced
);

/// <summary>KPIs do topo da tela — somados sobre as mesmas linhas devolvidas em Rows.</summary>
public record ResultadoResumo(
    decimal TotalProfit,
    decimal TotalRevenue,
    decimal AverageMargin,
    int AnalysedCount
)
{
    public static readonly ResultadoResumo Vazio = new(0m, 0m, 0m, 0);
}

public record ResultadoListResult(
    IReadOnlyList<ResultadoRow> Rows,
    ResultadoResumo Totals
);
