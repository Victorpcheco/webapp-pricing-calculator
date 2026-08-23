// Domain/Entities/Produtos/ItemComposicao.cs
namespace Domain.Entities.Produtos;

/// <summary>
/// Uma linha da ficha técnica: quanto de um insumo entra nesta produção.
/// Filho do agregado Produto — não existe fora dele.
/// </summary>
public class ItemComposicao
{
    /// <summary>
    /// Gerado pelo EF, não pelo domínio. Se o construtor preenchesse a chave,
    /// a heurística IsKeySet marcaria a linha nova como Modified em vez de Added
    /// e o EF emitiria UPDATE numa linha que ainda não existe.
    /// </summary>
    public Guid Id { get; private set; }

    public Guid InsumoId { get; private set; }

    /// <summary>Quantidade na unidade base do insumo (g, ml ou un).</summary>
    public decimal Quantidade { get; private set; }

    private ItemComposicao() { }

    internal ItemComposicao(Guid insumoId, decimal quantidade)
    {
        InsumoId = insumoId;
        Quantidade = quantidade;
    }
}
