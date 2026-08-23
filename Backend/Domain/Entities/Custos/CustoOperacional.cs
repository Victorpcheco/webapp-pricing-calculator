// Domain/Entities/Custos/CustoOperacional.cs
using Domain.Common;

namespace Domain.Entities.Custos;

public class CustoOperacional
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }

    public decimal ProLabore { get; private set; }
    public int HorasMensais { get; private set; }
    public decimal ContaEnergia { get; private set; }
    public decimal PercentualEnergiaTrabalho { get; private set; }
    public decimal GastoGas { get; private set; }
    public decimal PercentualGasTrabalho { get; private set; }
    public bool PossuiMei { get; private set; }
    public decimal ValorDas { get; private set; }
    public decimal TaxaDepreciacao { get; private set; }

    // Campos calculados — armazenados para exibição no histórico sem recalcular
    public decimal EnergiaReal { get; private set; }
    public decimal GasReal { get; private set; }
    public decimal ValorDepreciacao { get; private set; }
    public decimal CustoMensal { get; private set; }
    public decimal ValorHora { get; private set; }

    private CustoOperacional() { }

    public static Result<CustoOperacional> Criar(
        Guid usuarioId,
        string? descricao,
        decimal proLabore,
        int horasMensais,
        decimal contaEnergia,
        decimal percentualEnergiaTrabalho,
        decimal gastoGas,
        decimal percentualGasTrabalho,
        bool possuiMei,
        decimal valorDas,
        decimal taxaDepreciacao)
    {
        try { Validar(proLabore, horasMensais, percentualEnergiaTrabalho, percentualGasTrabalho, taxaDepreciacao); }
        catch (ArgumentException ex) { return Result<CustoOperacional>.Failure(ex.Message); }

        var custo = new CustoOperacional
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Descricao = string.IsNullOrWhiteSpace(descricao)
                ? $"Cálculo de {DateTime.UtcNow:dd/MM/yyyy}"
                : descricao,
            CriadoEm = DateTime.UtcNow
        };

        custo.AplicarValores(proLabore, horasMensais, contaEnergia, percentualEnergiaTrabalho,
            gastoGas, percentualGasTrabalho, possuiMei, valorDas, taxaDepreciacao);

        return Result<CustoOperacional>.Success(custo);
    }

    public Result Atualizar(
        decimal proLabore,
        int horasMensais,
        decimal contaEnergia,
        decimal percentualEnergiaTrabalho,
        decimal gastoGas,
        decimal percentualGasTrabalho,
        bool possuiMei,
        decimal valorDas,
        decimal taxaDepreciacao)
    {
        try { Validar(proLabore, horasMensais, percentualEnergiaTrabalho, percentualGasTrabalho, taxaDepreciacao); }
        catch (ArgumentException ex) { return Result.Failure(ex.Message); }

        AplicarValores(proLabore, horasMensais, contaEnergia, percentualEnergiaTrabalho,
            gastoGas, percentualGasTrabalho, possuiMei, valorDas, taxaDepreciacao);

        return Result.Success();
    }

    private void AplicarValores(
        decimal proLabore, int horasMensais,
        decimal contaEnergia, decimal percentualEnergiaTrabalho,
        decimal gastoGas, decimal percentualGasTrabalho,
        bool possuiMei, decimal valorDas, decimal taxaDepreciacao)
    {
        ProLabore = proLabore;
        HorasMensais = horasMensais;
        ContaEnergia = contaEnergia;
        PercentualEnergiaTrabalho = percentualEnergiaTrabalho;
        GastoGas = gastoGas;
        PercentualGasTrabalho = percentualGasTrabalho;
        PossuiMei = possuiMei;
        // Valor bruto preservado; zero é aplicado apenas no cálculo quando possuiMei = false
        ValorDas = valorDas;
        TaxaDepreciacao = taxaDepreciacao;

        EnergiaReal = contaEnergia * (percentualEnergiaTrabalho / 100m);
        GasReal = gastoGas * (percentualGasTrabalho / 100m);
        var dasEfetivo = possuiMei ? valorDas : 0m;
        var custoBase = EnergiaReal + GasReal + dasEfetivo + proLabore;
        ValorDepreciacao = custoBase * (taxaDepreciacao / 100m);
        CustoMensal = custoBase + ValorDepreciacao;
        ValorHora = horasMensais > 0 ? CustoMensal / horasMensais : 0m;
    }

    private static void Validar(
        decimal proLabore, int horasMensais,
        decimal percentualEnergia, decimal percentualGas, decimal taxaDepreciacao)
    {
        if (proLabore <= 0)
            throw new ArgumentException("O pró-labore deve ser maior que zero.");
        if (horasMensais < 1 || horasMensais > 744)
            throw new ArgumentException("As horas mensais devem estar entre 1 e 744.");
        if (percentualEnergia < 0 || percentualEnergia > 100)
            throw new ArgumentException("O percentual de energia deve estar entre 0 e 100.");
        if (percentualGas < 0 || percentualGas > 100)
            throw new ArgumentException("O percentual de gás deve estar entre 0 e 100.");
        if (taxaDepreciacao < 0 || taxaDepreciacao > 100)
            throw new ArgumentException("A taxa de depreciação deve estar entre 0 e 100.");
    }
}
