using Domain.Entities.Precificacoes;
using FluentAssertions;

namespace Tests.Precificacoes;

/// <summary>
/// Regras da simulação de preço — o cálculo precisa bater exatamente com a
/// prévia em tempo real exibida no formulário do frontend.
/// </summary>
public class SimulacaoPrecoTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid ProdutoId = Guid.NewGuid();

    private static SimulacaoPreco NovaSimulacao(
        decimal custoBase = 2.782m,
        decimal margem = 40m,
        decimal precoPraticado = 3.90m,
        int quantidade = 30)
        => SimulacaoPreco.Criar(
            UsuarioId, ProdutoId, "Bolo de chocolate", custoBase, margem, precoPraticado, quantidade).Value;

    /* ===================== CÁLCULO ===================== */

    [Fact]
    public void Criar_DeveCalcularOPrecoSugeridoSobreOCusto()
    {
        NovaSimulacao().PrecoSugerido.Should().Be(3.8948m); // 2,782 * 1,40
    }

    [Fact]
    public void Criar_DeveCalcularOLucroUnitarioComoDiferencaEntrePrecoECusto()
    {
        NovaSimulacao().LucroUnitario.Should().Be(1.118m); // 3,90 - 2,782
    }

    [Fact]
    public void Criar_DeveCalcularAMargemRealSobreOPrecoPraticado()
    {
        NovaSimulacao().MargemReal.Should().Be(28.6667m); // 1,118 / 3,90 * 100
    }

    [Fact]
    public void Criar_ComPrecoPraticadoZero_DeveZerarAMargemRealAoInvesDeDividirPorZero()
    {
        NovaSimulacao(precoPraticado: 0m).MargemReal.Should().Be(0m);
    }

    [Fact]
    public void Criar_DeveCalcularReceitaELucroTotalMultiplicandoPelaQuantidade()
    {
        var simulacao = NovaSimulacao();

        simulacao.ReceitaEstimada.Should().Be(117m);     // 3,90 * 30
        simulacao.LucroTotalEstimado.Should().Be(33.54m); // 1,118 * 30
    }

    [Fact]
    public void Criar_ComPrecoAbaixoDoCusto_DeveDevolverLucroNegativo()
    {
        var simulacao = NovaSimulacao(custoBase: 5m, precoPraticado: 4m, quantidade: 10);

        simulacao.LucroUnitario.Should().Be(-1m);
        simulacao.LucroTotalEstimado.Should().Be(-10m);
    }

    [Fact]
    public void Criar_ComCustoBaseNegativo_DeveZerarAoInvesDeInverterOSinal()
    {
        // Custo vem de uma consulta interna, nunca de entrada do usuário — mas a defesa existe
        var simulacao = SimulacaoPreco.Criar(UsuarioId, ProdutoId, "Bolo", -5m, 0m, 10m, 1).Value;

        simulacao.CustoBase.Should().Be(0m);
    }

    /* ===================== VALIDAÇÕES ===================== */

    [Fact]
    public void Criar_SemProduto_DeveFalhar()
    {
        var resultado = SimulacaoPreco.Criar(UsuarioId, Guid.Empty, "Bolo", 2.78m, 40m, 3.9m, 30);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("Selecione um produto para simular o preço.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_SemNomeDoProduto_DeveFalhar(string? nome)
    {
        var resultado = SimulacaoPreco.Criar(UsuarioId, ProdutoId, nome, 2.78m, 40m, 3.9m, 30);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O nome do produto é obrigatório.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Criar_ComMargemForaDoIntervalo_DeveFalhar(decimal margem)
    {
        var resultado = SimulacaoPreco.Criar(UsuarioId, ProdutoId, "Bolo", 2.78m, margem, 3.9m, 30);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_ComPrecoPraticadoNegativo_DeveFalhar()
    {
        var resultado = SimulacaoPreco.Criar(UsuarioId, ProdutoId, "Bolo", 2.78m, 40m, -0.01m, 30);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O preço praticado não pode ser negativo.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Criar_ComQuantidadeAbaixoDoMinimo_DeveFalhar(int quantidade)
    {
        var resultado = SimulacaoPreco.Criar(UsuarioId, ProdutoId, "Bolo", 2.78m, 40m, 3.9m, quantidade);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_ComNomeAcimaDoLimite_DeveFalhar()
    {
        var resultado = SimulacaoPreco.Criar(
            UsuarioId, ProdutoId, new string('a', SimulacaoPreco.NomeProdutoTamanhoMaximo + 1), 2.78m, 40m, 3.9m, 30);

        resultado.IsFailure.Should().BeTrue();
    }

    /* ===================== ATUALIZAÇÃO ===================== */

    [Fact]
    public void Atualizar_DeveRecalcularTodosOsValoresDerivados()
    {
        var simulacao = NovaSimulacao();
        var novoProdutoId = Guid.NewGuid();

        var resultado = simulacao.Atualizar(novoProdutoId, "Brigadeiro tradicional", 0.86m, 100m, 2.0m, 100);

        resultado.IsSuccess.Should().BeTrue();
        simulacao.ProdutoId.Should().Be(novoProdutoId);
        simulacao.ProdutoNome.Should().Be("Brigadeiro tradicional");
        simulacao.PrecoSugerido.Should().Be(1.72m);
        simulacao.LucroUnitario.Should().Be(1.14m);
        simulacao.MargemReal.Should().Be(57m);
        simulacao.ReceitaEstimada.Should().Be(200m);
        simulacao.LucroTotalEstimado.Should().Be(114m);
    }

    [Fact]
    public void Atualizar_ComDadosInvalidos_NaoDeveAlterarASimulacao()
    {
        var simulacao = NovaSimulacao();

        var resultado = simulacao.Atualizar(ProdutoId, "Bolo", 2.78m, 40m, 3.9m, 0);

        resultado.IsFailure.Should().BeTrue();
        simulacao.Quantidade.Should().Be(30);
        simulacao.LucroTotalEstimado.Should().Be(33.54m);
    }

    /* ===================== NORMALIZAÇÃO ===================== */

    [Fact]
    public void Criar_DeveRemoverEspacosSobrandoDoNomeDoProduto()
    {
        var simulacao = SimulacaoPreco.Criar(UsuarioId, ProdutoId, "  Bolo de chocolate  ", 2.78m, 40m, 3.9m, 30).Value;

        simulacao.ProdutoNome.Should().Be("Bolo de chocolate");
    }
}
