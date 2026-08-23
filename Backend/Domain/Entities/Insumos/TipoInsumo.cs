// Domain/Entities/Insumos/TipoInsumo.cs
namespace Domain.Entities.Insumos;

public enum TipoInsumo
{
    Ingrediente = 1,
    Embalagem = 2
}

public static class TipoInsumoExtensions
{
    /// <summary>Token exato esperado pelo frontend (item.type === 'Ingrediente').</summary>
    public static string Codigo(this TipoInsumo tipo) => tipo switch
    {
        TipoInsumo.Ingrediente => "Ingrediente",
        TipoInsumo.Embalagem => "Embalagem",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    public static bool TryParse(string? valor, out TipoInsumo tipo)
    {
        switch (valor?.Trim())
        {
            case "Ingrediente":
                tipo = TipoInsumo.Ingrediente;
                return true;
            case "Embalagem":
                tipo = TipoInsumo.Embalagem;
                return true;
            default:
                tipo = default;
                return false;
        }
    }
}
