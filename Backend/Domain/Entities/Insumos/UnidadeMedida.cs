// Domain/Entities/Insumos/UnidadeMedida.cs
namespace Domain.Entities.Insumos;

public enum UnidadeMedida
{
    Quilograma = 1,
    Grama = 2,
    Litro = 3,
    Mililitro = 4,
    Unidade = 5
}

/// <summary>
/// Regra de padronização de unidades — o coração do cálculo de custo do MeuPreço.
/// Toda compra é convertida para a unidade base (g, ml ou un) antes de dividir o preço.
/// </summary>
public static class UnidadeMedidaExtensions
{
    /// <summary>Token exato trafegado com o frontend (kg, g, L, ml, un).</summary>
    public static string Codigo(this UnidadeMedida unidade) => unidade switch
    {
        UnidadeMedida.Quilograma => "kg",
        UnidadeMedida.Grama => "g",
        UnidadeMedida.Litro => "L",
        UnidadeMedida.Mililitro => "ml",
        UnidadeMedida.Unidade => "un",
        _ => throw new ArgumentOutOfRangeException(nameof(unidade))
    };

    /// <summary>Fator multiplicador para converter a quantidade comprada na unidade base.</summary>
    public static decimal Fator(this UnidadeMedida unidade) => unidade switch
    {
        UnidadeMedida.Quilograma => 1000m,
        UnidadeMedida.Litro => 1000m,
        UnidadeMedida.Grama => 1m,
        UnidadeMedida.Mililitro => 1m,
        UnidadeMedida.Unidade => 1m,
        _ => throw new ArgumentOutOfRangeException(nameof(unidade))
    };

    /// <summary>Unidade em que o custo unitário é expresso: kg→g, L→ml, un→un.</summary>
    public static UnidadeMedida UnidadeBase(this UnidadeMedida unidade) => unidade switch
    {
        UnidadeMedida.Quilograma => UnidadeMedida.Grama,
        UnidadeMedida.Grama => UnidadeMedida.Grama,
        UnidadeMedida.Litro => UnidadeMedida.Mililitro,
        UnidadeMedida.Mililitro => UnidadeMedida.Mililitro,
        UnidadeMedida.Unidade => UnidadeMedida.Unidade,
        _ => throw new ArgumentOutOfRangeException(nameof(unidade))
    };

    public static bool TryParse(string? valor, out UnidadeMedida unidade)
    {
        // Comparação sensível a caixa: "L" (litro) e "ml" (mililitro) são tokens distintos.
        switch (valor?.Trim())
        {
            case "kg":
                unidade = UnidadeMedida.Quilograma;
                return true;
            case "g":
                unidade = UnidadeMedida.Grama;
                return true;
            case "L":
                unidade = UnidadeMedida.Litro;
                return true;
            case "ml":
                unidade = UnidadeMedida.Mililitro;
                return true;
            case "un":
                unidade = UnidadeMedida.Unidade;
                return true;
            default:
                unidade = default;
                return false;
        }
    }
}
