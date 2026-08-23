using Domain.Entities.Colaboradores;
using FluentAssertions;

namespace Tests.Colaboradores;

/// <summary>
/// Regras do domínio de colaboradores — em especial a provisão de encargos CLT,
/// que precisa bater exatamente com a prévia exibida no modal do frontend.
/// </summary>
public class ColaboradorTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private static Colaborador NovoClt(decimal salario = 1900m)
        => Colaborador.Criar(
            UsuarioId, "COL-01", "Juliana Ferreira", "Confeiteira",
            TipoContratacao.Clt, StatusColaborador.Ativo, null, salario, null, "(11) 98888-1234").Value;

    private static Colaborador NovoFreelancer(
        decimal valor = 45m,
        FrequenciaFreelancer frequencia = FrequenciaFreelancer.PorHora)
        => Colaborador.Criar(
            UsuarioId, "COL-02", "Rafael Souza", "Designer de embalagens",
            TipoContratacao.Freelancer, StatusColaborador.Ativo, null, valor, frequencia, null).Value;

    /* ===================== PROVISÃO CLT ===================== */

    [Fact]
    public void ProvisaoClt_DeveSeguirAsAliquotasPrevistasEmLei()
    {
        var provisao = ProvisaoClt.Calcular(1900m);

        provisao.Fgts.Should().Be(152m);                 // 8%
        provisao.DecimoTerceiro.Should().Be(158.3333m);  // 1/12
        provisao.Ferias.Should().Be(158.3333m);          // 1/12
        provisao.UmTercoFerias.Should().Be(52.7778m);    // 1/36
        provisao.Total.Should().Be(521.4444m);
    }

    [Fact]
    public void ProvisaoClt_DeveFecharComASomaDasParcelas()
    {
        var provisao = ProvisaoClt.Calcular(3187.42m);

        provisao.Total.Should().Be(
            provisao.Fgts + provisao.DecimoTerceiro + provisao.Ferias + provisao.UmTercoFerias);
    }

    [Fact]
    public void ProvisaoClt_ComSalarioNegativo_DeveZerarAoInvesDeInverterOSinal()
    {
        ProvisaoClt.Calcular(-500m).Should().Be(ProvisaoClt.Zero);
    }

    [Fact]
    public void Criar_ColaboradorClt_DeveExporOsEncargosProvisionados()
    {
        NovoClt().Provisao.Total.Should().Be(521.4444m);
    }

    [Fact]
    public void Criar_Freelancer_NaoDeveGerarEncargosTrabalhistas()
    {
        NovoFreelancer().Provisao.Should().Be(ProvisaoClt.Zero);
    }

    /* ===================== CUSTO MENSAL ===================== */

    [Fact]
    public void CustoMensal_DoClt_DeveSomarSalarioMaisEncargos()
    {
        NovoClt().CustoMensal.Should().Be(2421.4444m);
    }

    [Fact]
    public void CustoMensal_DoFreelancerMensal_DeveSerOValorCombinado()
    {
        NovoFreelancer(2500m, FrequenciaFreelancer.Mensal).CustoMensal.Should().Be(2500m);
    }

    [Theory]
    [InlineData(FrequenciaFreelancer.PorHora)]
    [InlineData(FrequenciaFreelancer.PorServico)]
    public void CustoMensal_DoFreelancerSobDemanda_DeveSerZero(FrequenciaFreelancer frequencia)
    {
        // Sem volume contratado não há projeção mensal confiável
        NovoFreelancer(45m, frequencia).CustoMensal.Should().Be(0m);
    }

    /* ===================== FORMA DE PAGAMENTO ===================== */

    [Fact]
    public void Criar_Clt_DeveIgnorarAFormaDePagamentoRecebida()
    {
        var colaborador = Colaborador.Criar(
            UsuarioId, null, "Juliana", "Confeiteira", TipoContratacao.Clt, StatusColaborador.Ativo,
            null, 1900m, FrequenciaFreelancer.PorHora, null).Value;

        colaborador.FrequenciaPagamento.Should().BeNull();
    }

    [Fact]
    public void Criar_FreelancerSemFormaDePagamento_DeveAssumirMensal()
    {
        var colaborador = Colaborador.Criar(
            UsuarioId, null, "Rafael", "Designer", TipoContratacao.Freelancer, StatusColaborador.Ativo,
            null, 45m, null, null).Value;

        colaborador.FrequenciaPagamento.Should().Be(FrequenciaFreelancer.Mensal);
    }

    /* ===================== CAMPOS OPCIONAIS ===================== */

    [Fact]
    public void Criar_SemDataDeAdmissao_DeveUsarADataDeHoje()
    {
        NovoClt().DataAdmissao.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_ComCodigoEmBranco_DeveGravarNull(string? codigo)
    {
        var colaborador = Colaborador.Criar(
            UsuarioId, codigo, "Juliana", "Confeiteira", TipoContratacao.Clt, StatusColaborador.Ativo,
            null, 1900m, null, codigo).Value;

        colaborador.Codigo.Should().BeNull();
        colaborador.Telefone.Should().BeNull();
    }

    [Fact]
    public void Criar_DeveRemoverEspacosSobrandoDoNomeEDoCargo()
    {
        var colaborador = Colaborador.Criar(
            UsuarioId, "  COL-01  ", "  Juliana Ferreira  ", "  Confeiteira  ",
            TipoContratacao.Clt, StatusColaborador.Ativo, null, 1900m, null, "  (11) 98888-1234  ").Value;

        colaborador.Codigo.Should().Be("COL-01");
        colaborador.Nome.Should().Be("Juliana Ferreira");
        colaborador.Cargo.Should().Be("Confeiteira");
        colaborador.Telefone.Should().Be("(11) 98888-1234");
    }

    /* ===================== VALIDAÇÕES ===================== */

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_SemNome_DeveFalhar(string? nome)
    {
        var resultado = Colaborador.Criar(
            UsuarioId, null, nome, "Confeiteira", TipoContratacao.Clt, StatusColaborador.Ativo,
            null, 1900m, null, null);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O nome do colaborador é obrigatório.");
    }

    [Fact]
    public void Criar_SemCargo_DeveFalhar()
    {
        var resultado = Colaborador.Criar(
            UsuarioId, null, "Juliana", " ", TipoContratacao.Clt, StatusColaborador.Ativo,
            null, 1900m, null, null);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O cargo do colaborador é obrigatório.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Criar_ComValorBaseNaoPositivo_DeveFalhar(decimal valorBase)
    {
        var resultado = Colaborador.Criar(
            UsuarioId, null, "Juliana", "Confeiteira", TipoContratacao.Clt, StatusColaborador.Ativo,
            null, valorBase, null, null);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O valor base deve ser maior que zero.");
    }

    [Fact]
    public void Criar_ComNomeAcimaDoLimite_DeveFalhar()
    {
        var resultado = Colaborador.Criar(
            UsuarioId, null, new string('a', Colaborador.NomeTamanhoMaximo + 1), "Confeiteira",
            TipoContratacao.Clt, StatusColaborador.Ativo, null, 1900m, null, null);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_ComCargoAcimaDoLimite_DeveFalhar()
    {
        var resultado = Colaborador.Criar(
            UsuarioId, null, "Juliana", new string('a', Colaborador.CargoTamanhoMaximo + 1),
            TipoContratacao.Clt, StatusColaborador.Ativo, null, 1900m, null, null);

        resultado.IsFailure.Should().BeTrue();
    }

    /* ===================== ATUALIZAÇÃO ===================== */

    [Fact]
    public void Atualizar_DeCltParaFreelancer_DeveZerarOsEncargosEReprovisionarOCusto()
    {
        var colaborador = NovoClt();

        var resultado = colaborador.Atualizar(
            "COL-01", "Juliana Ferreira", "Confeiteira", TipoContratacao.Freelancer,
            StatusColaborador.Ativo, null, 1900m, FrequenciaFreelancer.Mensal, null);

        resultado.IsSuccess.Should().BeTrue();
        colaborador.Provisao.Should().Be(ProvisaoClt.Zero);
        colaborador.CustoMensal.Should().Be(1900m);
        colaborador.FrequenciaPagamento.Should().Be(FrequenciaFreelancer.Mensal);
    }

    [Fact]
    public void Atualizar_DeFreelancerParaClt_DevePassarAProvisionarEncargos()
    {
        var colaborador = NovoFreelancer();

        colaborador.Atualizar(
            null, "Rafael Souza", "Designer", TipoContratacao.Clt,
            StatusColaborador.Ativo, null, 1900m, FrequenciaFreelancer.PorHora, null);

        colaborador.FrequenciaPagamento.Should().BeNull();
        colaborador.CustoMensal.Should().Be(2421.4444m);
    }

    [Fact]
    public void Atualizar_ComDadosInvalidos_NaoDeveAlterarOColaborador()
    {
        var colaborador = NovoClt();

        var resultado = colaborador.Atualizar(
            null, "Juliana", "Confeiteira", TipoContratacao.Clt, StatusColaborador.Ativo, null, 0m, null, null);

        resultado.IsFailure.Should().BeTrue();
        colaborador.ValorBase.Should().Be(1900m);
        colaborador.Codigo.Should().Be("COL-01");
    }
}
