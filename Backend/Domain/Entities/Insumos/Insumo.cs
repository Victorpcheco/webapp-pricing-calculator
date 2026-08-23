// Domain/Entities/Insumos/Insumo.cs
using Domain.Common;

namespace Domain.Entities.Insumos;

public class Insumo
{
    public const int NomeTamanhoMaximo = 80;
    public const decimal QuantidadeMinima = 0.001m;

    /// <summary>Casas decimais do custo unitário — insumos baratos por grama zerariam com 2.</summary>
    private const int CasasDecimaisCustoUnitario = 6;

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public TipoInsumo Tipo { get; private set; }

    public decimal Quantidade { get; private set; }
    public UnidadeMedida Unidade { get; private set; }
    public decimal Preco { get; private set; }

    // Campos calculados — armazenados para não recalcular a cada leitura da lista
    public decimal QuantidadeBase { get; private set; }
    public UnidadeMedida UnidadeBase { get; private set; }
    public decimal PrecoUnitario { get; private set; }

    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private Insumo() { }

    public static Result<Insumo> Criar(
        Guid usuarioId,
        string? nome,
        TipoInsumo tipo,
        decimal quantidade,
        UnidadeMedida unidade,
        decimal preco)
    {
        try { Validar(nome, quantidade, preco); }
        catch (ArgumentException ex) { return Result<Insumo>.Failure(ex.Message); }

        var agora = DateTime.UtcNow;

        var insumo = new Insumo
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            CriadoEm = agora,
            AtualizadoEm = agora
        };

        insumo.AplicarValores(nome!, tipo, quantidade, unidade, preco);

        return Result<Insumo>.Success(insumo);
    }

    public Result Atualizar(
        string? nome,
        TipoInsumo tipo,
        decimal quantidade,
        UnidadeMedida unidade,
        decimal preco)
    {
        try { Validar(nome, quantidade, preco); }
        catch (ArgumentException ex) { return Result.Failure(ex.Message); }

        AplicarValores(nome!, tipo, quantidade, unidade, preco);
        AtualizadoEm = DateTime.UtcNow;

        return Result.Success();
    }

    private void AplicarValores(
        string nome,
        TipoInsumo tipo,
        decimal quantidade,
        UnidadeMedida unidade,
        decimal preco)
    {
        Nome = nome.Trim();
        Tipo = tipo;
        Quantidade = quantidade;
        Unidade = unidade;
        Preco = preco;

        QuantidadeBase = quantidade * unidade.Fator();
        UnidadeBase = unidade.UnidadeBase();
        PrecoUnitario = QuantidadeBase > 0
            ? Math.Round(preco / QuantidadeBase, CasasDecimaisCustoUnitario, MidpointRounding.AwayFromZero)
            : 0m;
    }

    private static void Validar(string? nome, decimal quantidade, decimal preco)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome do insumo é obrigatório.");
        if (nome.Trim().Length > NomeTamanhoMaximo)
            throw new ArgumentException($"O nome deve ter no máximo {NomeTamanhoMaximo} caracteres.");
        if (quantidade < QuantidadeMinima)
            throw new ArgumentException($"A quantidade comprada deve ser de no mínimo {QuantidadeMinima:0.###}.");
        if (preco <= 0)
            throw new ArgumentException("O preço total pago deve ser maior que zero.");
    }
}
