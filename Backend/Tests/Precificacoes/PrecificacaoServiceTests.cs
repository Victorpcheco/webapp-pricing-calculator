using Application.Common;
using Application.Precificacoes.Commands;
using Application.Precificacoes.Queries;
using Application.Precificacoes.Services;
using Application.Repositories;
using Domain.Entities.Insumos;
using Domain.Entities.Precificacoes;
using Domain.Entities.Produtos;
using FluentAssertions;
using Moq;

namespace Tests.Precificacoes;

public class PrecificacaoServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly Mock<IPrecificacaoRepository> _precificacaoRepositoryMock = new();
    private readonly Mock<IProdutoRepository> _produtoRepositoryMock = new();
    private readonly Mock<IInsumoRepository> _insumoRepositoryMock = new();
    private readonly Mock<ICustoRepository> _custoRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly PrecificacaoService _sut;

    private readonly Insumo _farinha;
    private readonly Produto _bolo;

    public PrecificacaoServiceTests()
    {
        _currentUserMock.SetupGet(x => x.UsuarioId).Returns(UsuarioId);

        // Custo unitário 0,00498/g — mesmo insumo usado nos testes de Produtos
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
            .Setup(x => x.ObterPorIdAsync(_bolo.Id, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_bolo);

        _sut = new PrecificacaoService(
            _precificacaoRepositoryMock.Object,
            _produtoRepositoryMock.Object,
            _insumoRepositoryMock.Object,
            _custoRepositoryMock.Object,
            _currentUserMock.Object);
    }

    /// <summary>Custo unitário do _bolo com os mocks acima: (500 * 0,00498 + 60/60 * 19,94) / 10 = 2,2430.</summary>
    private const decimal CustoUnitarioEsperado = 2.2430m;

    private static SimulacaoPreco NovaSimulacao(Guid produtoId, decimal custoBase = 2.243m)
        => SimulacaoPreco.Criar(UsuarioId, produtoId, "Bolo de chocolate", custoBase, 40m, 3.9m, 30).Value;

    /* ===================== LISTAGEM ===================== */

    [Fact]
    public async Task ListarAsync_DeveMapearAsSimulacoesComOsTokensEsperadosPeloFrontend()
    {
        _precificacaoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { NovaSimulacao(_bolo.Id) });

        var resultado = await _sut.ListarAsync(new ListarSimulacoesQuery());

        var item = resultado.Single();
        item.RecipeId.Should().Be(_bolo.Id);
        item.RecipeName.Should().Be("Bolo de chocolate");
        item.Cost.Should().Be(2.243m);
        item.Suggested.Should().Be(3.1402m); // 2,243 * 1,40
    }

    /* ===================== CRIAÇÃO ===================== */

    [Fact]
    public async Task CriarAsync_DeveResolverOCustoAtualDoProdutoAoInvesDeConfiarNoCliente()
    {
        var command = new CriarSimulacaoCommand(_bolo.Id, 40m, 3.9m, 30);

        var resultado = await _sut.CriarAsync(command);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Cost.Should().Be(CustoUnitarioEsperado);
        resultado.Value.RecipeName.Should().Be("Bolo de chocolate");
        _precificacaoRepositoryMock.Verify(
            x => x.AdicionarAsync(It.IsAny<SimulacaoPreco>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_DeProdutoInexistente_DeveFalharSemTocarNoRepositorio()
    {
        var command = new CriarSimulacaoCommand(Guid.NewGuid(), 40m, 3.9m, 30);

        var resultado = await _sut.CriarAsync(command);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("Produto não encontrado.");
        _precificacaoRepositoryMock.Verify(
            x => x.AdicionarAsync(It.IsAny<SimulacaoPreco>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_ComMargemForaDoIntervalo_DevePropagarOErroDoDominio()
    {
        var command = new CriarSimulacaoCommand(_bolo.Id, 1500m, 3.9m, 30);

        var resultado = await _sut.CriarAsync(command);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task CriarAsync_DeProdutoDeOutroUsuario_DeveFalhar()
    {
        _produtoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(_bolo.Id, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Produto?)null);

        var resultado = await _sut.CriarAsync(new CriarSimulacaoCommand(_bolo.Id, 40m, 3.9m, 30));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("Produto não encontrado.");
    }

    /* ===================== ATUALIZAÇÃO ===================== */

    [Fact]
    public async Task AtualizarAsync_DeveReprovisionarOCustoComOProdutoAtualizado()
    {
        var simulacao = NovaSimulacao(_bolo.Id, custoBase: 1m);
        _precificacaoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(simulacao.Id, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(simulacao);

        var command = new AtualizarSimulacaoCommand(simulacao.Id, _bolo.Id, 50m, 4.5m, 20);

        var resultado = await _sut.AtualizarAsync(command);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Cost.Should().Be(CustoUnitarioEsperado);
        resultado.Value.Margin.Should().Be(50m);
        resultado.Value.Quantity.Should().Be(20);
        _precificacaoRepositoryMock.Verify(
            x => x.AtualizarAsync(simulacao, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_DeSimulacaoInexistente_DeveFalhar()
    {
        _precificacaoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>(), UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SimulacaoPreco?)null);

        var resultado = await _sut.AtualizarAsync(new AtualizarSimulacaoCommand(Guid.NewGuid(), _bolo.Id, 40m, 3.9m, 30));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("Simulação não encontrada.");
    }

    /* ===================== EXCLUSÃO ===================== */

    [Fact]
    public async Task ExcluirAsync_DeveRemoverASimulacaoDoUsuario()
    {
        var simulacao = NovaSimulacao(_bolo.Id);
        _precificacaoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(simulacao.Id, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(simulacao);

        var resultado = await _sut.ExcluirAsync(new ExcluirSimulacaoCommand(simulacao.Id));

        resultado.IsSuccess.Should().BeTrue();
        _precificacaoRepositoryMock.Verify(
            x => x.RemoverAsync(simulacao, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcluirAsync_DeSimulacaoDeOutroUsuario_DeveFalhar()
    {
        _precificacaoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>(), UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SimulacaoPreco?)null);

        var resultado = await _sut.ExcluirAsync(new ExcluirSimulacaoCommand(Guid.NewGuid()));

        resultado.IsFailure.Should().BeTrue();
        _precificacaoRepositoryMock.Verify(
            x => x.RemoverAsync(It.IsAny<SimulacaoPreco>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LimparAsync_DeveApagarApenasAsSimulacoesDoUsuarioAutenticado()
    {
        var resultado = await _sut.LimparAsync(new LimparSimulacoesCommand());

        resultado.IsSuccess.Should().BeTrue();
        _precificacaoRepositoryMock.Verify(
            x => x.RemoverTodosPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
