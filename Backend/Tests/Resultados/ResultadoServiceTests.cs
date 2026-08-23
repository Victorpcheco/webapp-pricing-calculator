using Application.Common;
using Application.Repositories;
using Application.Resultados.Queries;
using Application.Resultados.Services;
using Domain.Entities.Insumos;
using Domain.Entities.Precificacoes;
using Domain.Entities.Produtos;
using FluentAssertions;
using Moq;

namespace Tests.Resultados;

public class ResultadoServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly Mock<IProdutoRepository> _produtoRepositoryMock = new();
    private readonly Mock<IInsumoRepository> _insumoRepositoryMock = new();
    private readonly Mock<ICustoRepository> _custoRepositoryMock = new();
    private readonly Mock<IPrecificacaoRepository> _precificacaoRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly ResultadoService _sut;

    private readonly Insumo _farinha;
    private readonly Produto _bolo;

    public ResultadoServiceTests()
    {
        _currentUserMock.SetupGet(x => x.UsuarioId).Returns(UsuarioId);

        // Custo unitário 0,00498/g — mesmo insumo usado nos testes de Produtos/Precificação
        _farinha = Insumo.Criar(UsuarioId, "Farinha de trigo", TipoInsumo.Ingrediente,
            5m, UnidadeMedida.Quilograma, 24.90m).Value;

        _bolo = Produto.Criar(UsuarioId, "Bolo de chocolate", TipoProducao.Porcoes, 10, "fatia", 60,
            new[] { (_farinha.Id, 500m) }).Value;

        _custoRepositoryMock
            .Setup(x => x.ObterValorHoraAtualAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(19.94m);

        _insumoRepositoryMock
            .Setup(x => x.ListarPorIdsAsync(UsuarioId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                new[] { _farinha }.Where(i => ids.Contains(i.Id)).ToList());

        _produtoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { _bolo });

        _sut = new ResultadoService(
            _produtoRepositoryMock.Object,
            _insumoRepositoryMock.Object,
            _custoRepositoryMock.Object,
            _precificacaoRepositoryMock.Object,
            _currentUserMock.Object);
    }

    /// <summary>Custo unitário do _bolo com os mocks acima: (500 * 0,00498 + 60/60 * 19,94) / 10 = 2,2430.</summary>
    private const decimal CustoAtualDoBolo = 2.2430m;

    private void SemSimulacoes()
        => _precificacaoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SimulacaoPreco>());

    private SimulacaoPreco NovaSimulacao(decimal custoBase = 1m, decimal precoPraticado = 3.9m, int quantidade = 30)
        => SimulacaoPreco.Criar(UsuarioId, _bolo.Id, "Bolo de chocolate", custoBase, 40m, precoPraticado, quantidade).Value;

    private void ComSimulacoes(params SimulacaoPreco[] simulacoes)
        => _precificacaoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(simulacoes);

    /* ===================== SEM SIMULAÇÕES (fallback para receitas) ===================== */

    [Fact]
    public async Task ListarAsync_SemSimulacoes_DeveListarAsReceitasSoComOCusto()
    {
        SemSimulacoes();

        var resultado = await _sut.ListarAsync(new ListarResultadosQuery());

        var linha = resultado.Rows.Single();
        linha.Priced.Should().BeFalse();
        linha.Name.Should().Be("Bolo de chocolate");
        linha.Unit.Should().Be("fatia");
        linha.Cost.Should().Be(CustoAtualDoBolo);
        linha.SalePrice.Should().BeNull();
        linha.Profit.Should().BeNull();
        linha.Margin.Should().BeNull();
    }

    [Fact]
    public async Task ListarAsync_SemSimulacoesENenhumProduto_DeveDevolverListaVazia()
    {
        SemSimulacoes();
        _produtoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Produto>());

        var resultado = await _sut.ListarAsync(new ListarResultadosQuery());

        resultado.Rows.Should().BeEmpty();
        resultado.Totals.Should().Be(ResultadoResumo.Vazio);
    }

    /* ===================== COM SIMULAÇÕES ===================== */

    [Fact]
    public async Task ListarAsync_ComSimulacoes_DeveRecalcularLucroComOCustoAtualDoProduto()
    {
        // custoBase salvo na simulação (1,00) é velho — o produto hoje custa 2,2430
        ComSimulacoes(NovaSimulacao(custoBase: 1m, precoPraticado: 3.9m));

        var linha = (await _sut.ListarAsync(new ListarResultadosQuery())).Rows.Single();

        linha.Priced.Should().BeTrue();
        linha.Cost.Should().Be(CustoAtualDoBolo);
        linha.Profit.Should().Be(3.9m - CustoAtualDoBolo);
    }

    [Fact]
    public async Task ListarAsync_ComProdutoExcluido_DeveCairParaOCustoGravadoNaSimulacao()
    {
        var simulacao = NovaSimulacao(custoBase: 1.5m, precoPraticado: 3.9m);
        ComSimulacoes(simulacao);
        _produtoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Produto>());

        var linha = (await _sut.ListarAsync(new ListarResultadosQuery())).Rows.Single();

        linha.Cost.Should().Be(1.5m);
        linha.Profit.Should().Be(3.9m - 1.5m);
        linha.Unit.Should().Be("unidade"); // sem produto, não há nome de unidade para resolver
    }

    [Fact]
    public async Task ListarAsync_ComPrecoZero_DeveZerarAMargemAoInvesDeDividirPorZero()
    {
        ComSimulacoes(NovaSimulacao(precoPraticado: 0m));

        var linha = (await _sut.ListarAsync(new ListarResultadosQuery())).Rows.Single();

        linha.Margin.Should().Be(0m);
    }

    /* ===================== KPIs ===================== */

    [Fact]
    public async Task ListarAsync_DeveSomarReceitaELucroPonderadosPelaQuantidade()
    {
        // Custo atual do produto (2,2430) é o mesmo para as duas — o custoBase informado é só o retrato antigo
        ComSimulacoes(
            NovaSimulacao(precoPraticado: 3.9m, quantidade: 30),
            NovaSimulacao(precoPraticado: 5m, quantidade: 10));

        var totais = (await _sut.ListarAsync(new ListarResultadosQuery())).Totals;

        var receitaEsperada = 3.9m * 30 + 5m * 10; // 167
        var lucroEsperado = (3.9m - CustoAtualDoBolo) * 30 + (5m - CustoAtualDoBolo) * 10; // 77,979
        var margemEsperada = lucroEsperado / receitaEsperada * 100m;

        totais.AnalysedCount.Should().Be(2);
        totais.TotalRevenue.Should().Be(receitaEsperada);
        totais.TotalProfit.Should().Be(lucroEsperado);
        totais.AverageMargin.Should().Be(margemEsperada);
    }

    [Fact]
    public async Task ListarAsync_SemNenhumaSimulacaoOuReceita_DeveDevolverResumoVazio()
    {
        SemSimulacoes();
        _produtoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Produto>());

        var resultado = await _sut.ListarAsync(new ListarResultadosQuery());

        resultado.Totals.Should().Be(ResultadoResumo.Vazio);
    }

    /* ===================== FILTRO DE PERÍODO ===================== */

    [Fact]
    public async Task ListarAsync_PeriodoHoje_DeveExcluirSimulacoesDeOutrosDias()
    {
        var antiga = NovaSimulacao();
        DefinirCriadoEm(antiga, DateTime.UtcNow.AddDays(-3));
        ComSimulacoes(antiga);

        var resultado = await _sut.ListarAsync(new ListarResultadosQuery(Periodo: "today"));

        // Sem simulação no período, cai para a lista de receitas (o produto ainda existe)
        resultado.Rows.Single().Priced.Should().BeFalse();
    }

    [Fact]
    public async Task ListarAsync_PeriodoSemana_DeveIncluirSimulacaoDosUltimosSeteDias()
    {
        var recente = NovaSimulacao();
        DefinirCriadoEm(recente, DateTime.UtcNow.AddDays(-2));
        ComSimulacoes(recente);

        var resultado = await _sut.ListarAsync(new ListarResultadosQuery(Periodo: "week"));

        resultado.Rows.Single().Priced.Should().BeTrue();
    }

    [Fact]
    public async Task ListarAsync_PeriodoCustomizado_DeveRespeitarOIntervaloInformado()
    {
        var simulacao = NovaSimulacao();
        DefinirCriadoEm(simulacao, new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc));
        ComSimulacoes(simulacao);

        var foraDoIntervalo = await _sut.ListarAsync(new ListarResultadosQuery(
            Periodo: "custom", Inicio: new DateTime(2026, 4, 1), Fim: new DateTime(2026, 4, 30)));
        var dentroDoIntervalo = await _sut.ListarAsync(new ListarResultadosQuery(
            Periodo: "custom", Inicio: new DateTime(2026, 3, 1), Fim: new DateTime(2026, 3, 31)));

        // Fora do intervalo pedido, nem a simulação nem o produto (criado "agora") entram
        foraDoIntervalo.Rows.Should().BeEmpty();
        dentroDoIntervalo.Rows.Single().Priced.Should().BeTrue();
    }

    [Fact]
    public async Task ListarAsync_PeriodoTodos_DeveIncluirSimulacoesAntigas()
    {
        var antiga = NovaSimulacao();
        DefinirCriadoEm(antiga, DateTime.UtcNow.AddYears(-1));
        ComSimulacoes(antiga);

        var resultado = await _sut.ListarAsync(new ListarResultadosQuery(Periodo: "all"));

        resultado.Rows.Single().Priced.Should().BeTrue();
    }

    /// <summary>CriadoEm é somente leitura no domínio — ajustado via reflexão só para simular datas passadas nos testes.</summary>
    private static void DefinirCriadoEm(SimulacaoPreco simulacao, DateTime data)
    {
        typeof(SimulacaoPreco).GetProperty(nameof(SimulacaoPreco.CriadoEm))!
            .SetValue(simulacao, data);
    }
}
