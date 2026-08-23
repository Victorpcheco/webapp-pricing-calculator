// Application/Insumos/Services/InsumosResumo.cs
namespace Application.Insumos.Services;

/// <summary>
/// Totalizadores dos cards de estatística da tela.
/// Refletem o universo completo do usuário — nunca o recorte filtrado da busca.
/// </summary>
public record InsumosResumo(
    int Total,
    int IngredientCount,
    int PackageCount,
    decimal PurchaseValue
)
{
    public static readonly InsumosResumo Vazio = new(0, 0, 0, 0m);
}
