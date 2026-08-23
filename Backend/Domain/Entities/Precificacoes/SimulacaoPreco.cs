// Domain/Entities/Precificacoes/SimulacaoPreco.cs
using Domain.Common;

namespace Domain.Entities.Precificacoes;

/// <summary>
/// Registro de uma decisão de preço testada pelo usuário para um produto.
/// Histórico, não um cadastro vivo: nome e custo do produto são gravados
/// como estavam no momento do cálculo, e não mudam se o produto for
/// renomeado, tiver o custo alterado ou for excluído depois.
/// </summary>
public class SimulacaoPreco
{
    public const int NomeProdutoTamanhoMaximo = 80;

    public const decimal MargemMaxima = 1000m;
    public const int QuantidadeMinima = 1;

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }

    /// <summary>Referência solta ao produto — sem FK: o produto pode ser excluído sem apagar o histórico.</summary>
    public Guid ProdutoId { get; private set; }
    public string ProdutoNome { get; private set; } = string.Empty;

    /// <summary>Custo unitário do produto no momento em que a simulação foi salva.</summary>
    public decimal CustoBase { get; private set; }
    public decimal Margem { get; private set; }
    public decimal PrecoPraticado { get; private set; }
    public int Quantidade { get; private set; }

    // Campos calculados — armazenados para exibição no histórico sem recalcular
    public decimal PrecoSugerido { get; private set; }
    public decimal LucroUnitario { get; private set; }
    public decimal MargemReal { get; private set; }
    public decimal ReceitaEstimada { get; private set; }
    public decimal LucroTotalEstimado { get; private set; }

    public DateTime CriadoEm { get; private set; }

    private SimulacaoPreco() { }

    public static Result<SimulacaoPreco> Criar(
        Guid usuarioId,
        Guid produtoId,
        string? produtoNome,
        decimal custoBase,
        decimal margem,
        decimal precoPraticado,
        int quantidade)
    {
        try { Validar(produtoId, produtoNome, margem, precoPraticado, quantidade); }
        catch (ArgumentException ex) { return Result<SimulacaoPreco>.Failure(ex.Message); }

        var simulacao = new SimulacaoPreco
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            ProdutoId = produtoId,
            CriadoEm = DateTime.UtcNow
        };

        simulacao.AplicarValores(produtoNome!, custoBase, margem, precoPraticado, quantidade);

        return Result<SimulacaoPreco>.Success(simulacao);
    }

    public Result Atualizar(
        Guid produtoId,
        string? produtoNome,
        decimal custoBase,
        decimal margem,
        decimal precoPraticado,
        int quantidade)
    {
        try { Validar(produtoId, produtoNome, margem, precoPraticado, quantidade); }
        catch (ArgumentException ex) { return Result.Failure(ex.Message); }

        ProdutoId = produtoId;
        AplicarValores(produtoNome!, custoBase, margem, precoPraticado, quantidade);

        return Result.Success();
    }

    private void AplicarValores(string produtoNome, decimal custoBase, decimal margem, decimal precoPraticado, int quantidade)
    {
        ProdutoNome = produtoNome.Trim();

        // Custo vem de uma consulta interna (unit cost do produto), não de entrada do usuário: nunca é negativo
        CustoBase = Math.Max(0m, custoBase);
        Margem = margem;
        PrecoPraticado = precoPraticado;
        Quantidade = quantidade;

        PrecoSugerido = Arredondar(CustoBase * (1 + Margem / 100m));
        LucroUnitario = Arredondar(PrecoPraticado - CustoBase);
        MargemReal = PrecoPraticado > 0 ? Arredondar((LucroUnitario / PrecoPraticado) * 100m) : 0m;
        ReceitaEstimada = Arredondar(PrecoPraticado * Quantidade);
        LucroTotalEstimado = Arredondar(LucroUnitario * Quantidade);
    }

    private static void Validar(Guid produtoId, string? produtoNome, decimal margem, decimal precoPraticado, int quantidade)
    {
        if (produtoId == Guid.Empty)
            throw new ArgumentException("Selecione um produto para simular o preço.");

        if (string.IsNullOrWhiteSpace(produtoNome))
            throw new ArgumentException("O nome do produto é obrigatório.");
        if (produtoNome.Trim().Length > NomeProdutoTamanhoMaximo)
            throw new ArgumentException($"O nome do produto deve ter no máximo {NomeProdutoTamanhoMaximo} caracteres.");

        if (margem < 0 || margem > MargemMaxima)
            throw new ArgumentException($"A margem deve estar entre 0% e {MargemMaxima:0}%.");
        if (precoPraticado < 0)
            throw new ArgumentException("O preço praticado não pode ser negativo.");
        if (quantidade < QuantidadeMinima)
            throw new ArgumentException($"A quantidade estimada deve ser de no mínimo {QuantidadeMinima}.");
    }

    private static decimal Arredondar(decimal valor) =>
        Math.Round(valor, 4, MidpointRounding.AwayFromZero);
}
