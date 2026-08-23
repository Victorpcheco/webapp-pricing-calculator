// Domain/Entities/Produtos/FichaCalculada.cs
namespace Domain.Entities.Produtos;

/// <summary>Custo de uma linha da composição, já resolvido.</summary>
public record LinhaCalculada(
    Guid InsumoId,
    decimal Quantidade,
    decimal CustoUnitarioInsumo,
    decimal Custo
);

/// <summary>
/// Resultado do cálculo da ficha técnica. Nada disso é persistido:
/// depende do preço atual dos insumos e do valor da hora vigente.
/// </summary>
public record FichaCalculada(
    IReadOnlyList<LinhaCalculada> Linhas,
    decimal CustoMateriais,
    decimal CustoTrabalho,
    decimal CustoTotal,
    decimal CustoUnitario
);
