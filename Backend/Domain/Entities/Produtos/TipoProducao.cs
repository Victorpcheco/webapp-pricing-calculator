// Domain/Entities/Produtos/TipoProducao.cs
namespace Domain.Entities.Produtos;

public enum TipoProducao
{
    ProdutoInteiro = 1,
    Porcoes = 2
}

public static class TipoProducaoExtensions
{
    /// <summary>Token exato usado pelos radios do formulário.</summary>
    public static string Codigo(this TipoProducao tipo) => tipo switch
    {
        TipoProducao.ProdutoInteiro => "Produto inteiro",
        TipoProducao.Porcoes => "Porções",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    public static bool TryParse(string? valor, out TipoProducao tipo)
    {
        switch (valor?.Trim())
        {
            case "Produto inteiro":
                tipo = TipoProducao.ProdutoInteiro;
                return true;
            case "Porções":
                tipo = TipoProducao.Porcoes;
                return true;
            default:
                tipo = default;
                return false;
        }
    }
}
