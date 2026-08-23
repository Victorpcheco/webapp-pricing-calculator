using Domain.Entities.Custos;
using FluentAssertions;

namespace Tests.Custos;

public class CustoOperacionalTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    /// <summary>Valores padrão da tela: pró-labore 3000 em 176h, energia 450 a 30%, gás 180 a 70%, MEI com DAS 80,90 e 5% de depreciação.</summary>
    private static CustoOperacional CriarValido(
        string? descricao = "Configuração atual",
        decimal proLabore = 3000m,
        int horasMensais = 176,
        decimal contaEnergia = 450m,
        decimal percentualEnergia = 30m,
        decimal gastoGas = 180m,
        decimal percentualGas = 70m,
        bool possuiMei = true,
        decimal valorDas = 80.90m,
        decimal taxaDepreciacao = 5m)
    {
        var resultado = CustoOperacional.Criar(
            UsuarioId, descricao, proLabore, horasMensais, contaEnergia, percentualEnergia,
            gastoGas, percentualGas, possuiMei, valorDas, taxaDepreciacao);

        resultado.IsSuccess.Should().BeTrue(resultado.Error);
        return resultado.Value;
    }

    /* ===================== CÁLCULO ===================== */

    [Fact]
    public void Criar_DeveEncadearOCalculoAteOValorHora()
    {
        var custo = CriarValido();

        custo.EnergiaReal.Should().Be(135m);            // 450 × 30%
        custo.GasReal.Should().Be(126m);                // 180 × 70%
        custo.ValorDepreciacao.Should().Be(167.0950m);  // (135 + 126 + 80,90 + 3000) × 5%
        custo.CustoMensal.Should().Be(3508.9950m);
        custo.ValorHora.Should().BeApproximately(19.9375m, 0.0001m); // 3508,995 ÷ 176
    }

    [Fact]
    public void Criar_SemMei_DeveIgnorarODasNoCalculo()
    {
        var comMei = CriarValido(possuiMei: true, valorDas: 80.90m);
        var semMei = CriarValido(possuiMei: false, valorDas: 80.90m);

        (comMei.CustoMensal - semMei.CustoMensal).Should().Be(84.9450m); // 80,90 + 5% de depreciação
    }

    [Fact]
    public void Criar_SemMei_DevePreservarOValorDasInformado()
    {
        // O valor bruto continua salvo para não sumir do formulário
        // se o usuário voltar a marcar MEI depois
        var custo = CriarValido(possuiMei: false, valorDas: 80.90m);

        custo.ValorDas.Should().Be(80.90m);
        custo.PossuiMei.Should().BeFalse();
    }

    [Fact]
    public void Criar_SemDepreciacao_DeveIgualarCustoMensalAoCustoBase()
    {
        var custo = CriarValido(taxaDepreciacao: 0m);

        custo.ValorDepreciacao.Should().Be(0m);
        custo.CustoMensal.Should().Be(3341.90m); // 135 + 126 + 80,90 + 3000
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(50, 225)]
    [InlineData(100, 450)]
    public void Criar_DeveAplicarOPercentualSobreAContaDeEnergia(decimal percentual, decimal esperado)
    {
        var custo = CriarValido(contaEnergia: 450m, percentualEnergia: percentual);

        custo.EnergiaReal.Should().Be(esperado);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(50, 90)]
    [InlineData(100, 180)]
    public void Criar_DeveAplicarOPercentualSobreOGastoDeGas(decimal percentual, decimal esperado)
    {
        var custo = CriarValido(gastoGas: 180m, percentualGas: percentual);

        custo.GasReal.Should().Be(esperado);
    }

    [Fact]
    public void Criar_ComMaisHoras_DeveBaratearAHora()
    {
        var poucasHoras = CriarValido(horasMensais: 100);
        var muitasHoras = CriarValido(horasMensais: 200);

        muitasHoras.ValorHora.Should().BeLessThan(poucasHoras.ValorHora);
        muitasHoras.CustoMensal.Should().Be(poucasHoras.CustoMensal);
    }

    [Fact]
    public void Criar_SemDespesasExtras_DeveCobrarApenasOProLabore()
    {
        var custo = CriarValido(
            contaEnergia: 0m, percentualEnergia: 0m,
            gastoGas: 0m, percentualGas: 0m,
            possuiMei: false, valorDas: 0m, taxaDepreciacao: 0m,
            proLabore: 2200m, horasMensais: 200);

        custo.CustoMensal.Should().Be(2200m);
        custo.ValorHora.Should().Be(11m);
    }

    /* ===================== DESCRIÇÃO ===================== */

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_SemDescricao_DeveGerarUmaComADataDoDia(string? descricao)
    {
        var custo = CriarValido(descricao: descricao);

        custo.Descricao.Should().Be($"Cálculo de {DateTime.UtcNow:dd/MM/yyyy}");
    }

    [Fact]
    public void Criar_ComDescricao_DevePreservarOTextoInformado()
    {
        var custo = CriarValido(descricao: "Configuração de verão");

        custo.Descricao.Should().Be("Configuração de verão");
    }

    [Fact]
    public void Criar_DeveRegistrarUsuarioIdEDataDeCriacao()
    {
        var custo = CriarValido();

        custo.Id.Should().NotBeEmpty();
        custo.UsuarioId.Should().Be(UsuarioId);
        custo.CriadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /* ===================== VALIDAÇÕES ===================== */

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Criar_ComProLaboreNaoPositivo_DeveFalhar(decimal proLabore)
    {
        var resultado = CustoOperacional.Criar(
            UsuarioId, "x", proLabore, 176, 450m, 30m, 180m, 70m, true, 80.90m, 5m);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O pró-labore deve ser maior que zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(745)]
    public void Criar_ComHorasMensaisForaDoIntervalo_DeveFalhar(int horas)
    {
        var resultado = CustoOperacional.Criar(
            UsuarioId, "x", 3000m, horas, 450m, 30m, 180m, 70m, true, 80.90m, 5m);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("As horas mensais devem estar entre 1 e 744.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(744)]
    public void Criar_NosLimitesDeHorasMensais_DeveSerAceito(int horas)
    {
        // 744 = 31 dias × 24h, o mês mais longo possível
        var resultado = CustoOperacional.Criar(
            UsuarioId, "x", 3000m, horas, 450m, 30m, 180m, 70m, true, 80.90m, 5m);

        resultado.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Criar_ComPercentualDeEnergiaForaDoIntervalo_DeveFalhar(decimal percentual)
    {
        var resultado = CustoOperacional.Criar(
            UsuarioId, "x", 3000m, 176, 450m, percentual, 180m, 70m, true, 80.90m, 5m);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O percentual de energia deve estar entre 0 e 100.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Criar_ComPercentualDeGasForaDoIntervalo_DeveFalhar(decimal percentual)
    {
        var resultado = CustoOperacional.Criar(
            UsuarioId, "x", 3000m, 176, 450m, 30m, 180m, percentual, true, 80.90m, 5m);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O percentual de gás deve estar entre 0 e 100.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Criar_ComTaxaDeDepreciacaoForaDoIntervalo_DeveFalhar(decimal taxa)
    {
        var resultado = CustoOperacional.Criar(
            UsuarioId, "x", 3000m, 176, 450m, 30m, 180m, 70m, true, 80.90m, taxa);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("A taxa de depreciação deve estar entre 0 e 100.");
    }

    /* ===================== ATUALIZAÇÃO ===================== */

    [Fact]
    public void Atualizar_DeveRecalcularTodosOsDerivados()
    {
        var custo = CriarValido();

        var resultado = custo.Atualizar(
            proLabore: 4000m, horasMensais: 200,
            contaEnergia: 500m, percentualEnergiaTrabalho: 50m,
            gastoGas: 200m, percentualGasTrabalho: 50m,
            possuiMei: false, valorDas: 80.90m, taxaDepreciacao: 0m);

        resultado.IsSuccess.Should().BeTrue();
        custo.EnergiaReal.Should().Be(250m);
        custo.GasReal.Should().Be(100m);
        custo.CustoMensal.Should().Be(4350m); // 250 + 100 + 0 (sem MEI) + 4000
        custo.ValorHora.Should().Be(21.75m);
    }

    [Fact]
    public void Atualizar_NaoDeveAlterarADescricao()
    {
        // A descrição é definida na criação e o Atualizar nem recebe esse parâmetro
        var custo = CriarValido(descricao: "Configuração de verão");

        custo.Atualizar(4000m, 200, 500m, 50m, 200m, 50m, false, 0m, 0m);

        custo.Descricao.Should().Be("Configuração de verão");
    }

    [Fact]
    public void Atualizar_DevePreservarCriadoEm()
    {
        var custo = CriarValido();
        var criadoOriginal = custo.CriadoEm;

        custo.Atualizar(4000m, 200, 500m, 50m, 200m, 50m, false, 0m, 0m);

        custo.CriadoEm.Should().Be(criadoOriginal);
    }

    [Fact]
    public void Atualizar_ComDadosInvalidos_NaoDeveAlterarOEstado()
    {
        var custo = CriarValido();
        var mensalOriginal = custo.CustoMensal;

        var resultado = custo.Atualizar(0m, 200, 500m, 50m, 200m, 50m, false, 0m, 0m);

        resultado.IsFailure.Should().BeTrue();
        custo.ProLabore.Should().Be(3000m);
        custo.CustoMensal.Should().Be(mensalOriginal);
    }
}
