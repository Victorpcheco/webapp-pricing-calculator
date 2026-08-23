// Application/Produtos/Services/ProdutosListResult.cs
namespace Application.Produtos.Services;

/// <summary>
/// Totais da tela. O <c>HourlyRate</c> alimenta a prévia em tempo real do
/// formulário, que segue sendo calculada no cliente enquanto o usuário digita.
/// </summary>
public record ProdutosResumo(
    int Total,
    decimal HourlyRate
);

public record ProdutosListResult(
    IReadOnlyList<ProdutoResult> Data,
    ProdutosResumo Meta
);
