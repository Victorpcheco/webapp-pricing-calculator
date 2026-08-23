// Domain/Entities/Colaboradores/ProvisaoClt.cs
namespace Domain.Entities.Colaboradores;

/// <summary>
/// Provisão mensal dos encargos previstos em lei para um colaborador CLT.
/// Calculada sobre o salário bruto — é o mesmo cálculo da prévia do modal.
/// </summary>
public record ProvisaoClt(
    decimal Fgts,
    decimal DecimoTerceiro,
    decimal Ferias,
    decimal UmTercoFerias,
    decimal Total
)
{
    /// <summary>Freelancer não gera encargos trabalhistas.</summary>
    public static readonly ProvisaoClt Zero = new(0m, 0m, 0m, 0m, 0m);

    /// <summary>Casas decimais dos valores derivados — mesmo critério da ficha técnica.</summary>
    private const int CasasDecimais = 4;

    public const decimal AliquotaFgts = 0.08m;

    /// <summary>Divisor do 13º e das férias: 1/12 avos por mês trabalhado.</summary>
    public const decimal DivisorAvos = 12m;

    /// <summary>1/3 constitucional incide sobre o avo de férias: (salário ÷ 12) ÷ 3.</summary>
    public const decimal DivisorUmTercoFerias = 36m;

    public static ProvisaoClt Calcular(decimal salarioBruto)
    {
        var salario = Math.Max(0m, salarioBruto);

        var fgts = Arredondar(salario * AliquotaFgts);
        var decimoTerceiro = Arredondar(salario / DivisorAvos);
        var ferias = Arredondar(salario / DivisorAvos);
        var umTercoFerias = Arredondar(salario / DivisorUmTercoFerias);

        // Soma das parcelas já arredondadas: a lista de encargos da tela sempre fecha com o total
        return new ProvisaoClt(
            fgts,
            decimoTerceiro,
            ferias,
            umTercoFerias,
            fgts + decimoTerceiro + ferias + umTercoFerias
        );
    }

    private static decimal Arredondar(decimal valor) =>
        Math.Round(valor, CasasDecimais, MidpointRounding.AwayFromZero);
}
