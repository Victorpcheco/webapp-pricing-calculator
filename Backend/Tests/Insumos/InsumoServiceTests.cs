using Application.Common;
using Application.Insumos.Commands;
using Application.Insumos.Queries;
using Application.Insumos.Services;
using Application.Repositories;
using Domain.Entities.Insumos;
using FluentAssertions;
using Moq;
using Insumo = Domain.Entities.Insumos.Insumo;

namespace Tests.Insumos;

public class InsumoServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly Mock<IInsumoRepository> _insumoRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly InsumoService _sut;

    public InsumoServiceTests()
    {
        _insumoRepositoryMock = new Mock<IInsumoRepository>();
        _currentUserMock = new Mock<ICurrentUserService>();
        _currentUserMock.SetupGet(x => x.UsuarioId).Returns(UsuarioId);

        _insumoRepositoryMock
            .Setup(x => x.ObterResumoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(InsumosResumo.Vazio);

        _sut = new InsumoService(_insumoRepositoryMock.Object, _currentUserMock.Object);
    }

    private static Insumo NovoInsumo(
        string nome = "Farinha de trigo",
        TipoInsumo tipo = TipoInsumo.Ingrediente,
        decimal quantidade = 5m,
        UnidadeMedida unidade = UnidadeMedida.Quilograma,
        decimal preco = 24.90m)
        => Insumo.Criar(UsuarioId, nome, tipo, quantidade, unidade, preco).Value;

    /* ===================== LISTAGEM ===================== */

    [Fact]
    public async Task ListarAsync_DeveMapearOsItensComOsTokensEsperadosPeloFrontend()
    {
        _insumoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { NovoInsumo() });

        var resultado = await _sut.ListarAsync(new ListarInsumosQuery());

        resultado.IsSuccess.Should().BeTrue();
        var item = resultado.Value.Data.Single();
        item.Type.Should().Be("Ingrediente");
        item.Unit.Should().Be("kg");
        item.BaseUnit.Should().Be("g");
        item.BaseQuantity.Should().Be(5000m);
        item.UnitCost.Should().Be(0.00498m);
    }

    [Fact]
    public async Task ListarAsync_DeveRepassarOsFiltrosDaToolbarParaORepositorio()
    {
        _insumoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, "farinha", TipoInsumo.Ingrediente, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Insumo>());

        var resultado = await _sut.ListarAsync(new ListarInsumosQuery("farinha", "Ingrediente"));

        resultado.IsSuccess.Should().BeTrue();
        _insumoRepositoryMock.Verify(
            x => x.ListarPorUsuarioAsync(UsuarioId, "farinha", TipoInsumo.Ingrediente, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListarAsync_ComTipoInvalido_DeveFalharSemConsultarORepositorio()
    {
        var resultado = await _sut.ListarAsync(new ListarInsumosQuery(Tipo: "Bebida"));

        resultado.IsFailure.Should().BeTrue();
        _insumoRepositoryMock.Verify(
            x => x.ListarPorUsuarioAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TipoInsumo?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ListarAsync_DeveRetornarOsTotaisGlobaisMesmoComFiltroAplicado()
    {
        // Os cards de estatística refletem o universo completo, não o recorte filtrado
        _insumoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, "farinha", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { NovoInsumo() });
        _insumoRepositoryMock
            .Setup(x => x.ObterResumoAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InsumosResumo(12, 8, 4, 348.70m));

        var resultado = await _sut.ListarAsync(new ListarInsumosQuery("farinha"));

        resultado.Value.Data.Should().HaveCount(1);
        resultado.Value.Meta.Total.Should().Be(12);
        resultado.Value.Meta.IngredientCount.Should().Be(8);
        resultado.Value.Meta.PackageCount.Should().Be(4);
        resultado.Value.Meta.PurchaseValue.Should().Be(348.70m);
    }

    /* ===================== CRIAÇÃO ===================== */

    [Fact]
    public async Task CriarAsync_ComDadosValidos_DevePersistirNoEscopoDoUsuarioAutenticado()
    {
        var command = new CriarInsumoCommand("Farinha de trigo", "Ingrediente", 5m, "kg", 24.90m);

        var resultado = await _sut.CriarAsync(command);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.UnitCost.Should().Be(0.00498m);

        _insumoRepositoryMock.Verify(x => x.AdicionarAsync(
            It.Is<Insumo>(i => i.UsuarioId == UsuarioId && i.Nome == "Farinha de trigo"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("kg", 5, 5000, "g")]
    [InlineData("g", 500, 500, "g")]
    [InlineData("L", 2, 2000, "ml")]
    [InlineData("ml", 750, 750, "ml")]
    [InlineData("un", 50, 50, "un")]
    public async Task CriarAsync_DeveConverterCadaUnidadeParaSuaBase(
        string unidade, decimal quantidade, decimal baseEsperada, string unidadeBaseEsperada)
    {
        var command = new CriarInsumoCommand("Item", "Ingrediente", quantidade, unidade, 100m);

        var resultado = await _sut.CriarAsync(command);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.BaseQuantity.Should().Be(baseEsperada);
        resultado.Value.BaseUnit.Should().Be(unidadeBaseEsperada);
    }

    [Fact]
    public async Task CriarAsync_ComUnidadeInvalida_DeveFalharSemPersistir()
    {
        var command = new CriarInsumoCommand("Farinha", "Ingrediente", 5m, "arroba", 24.90m);

        var resultado = await _sut.CriarAsync(command);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("unidade");
        _insumoRepositoryMock.Verify(x => x.AdicionarAsync(It.IsAny<Insumo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_ComPrecoZero_DeveFalharSemPersistir()
    {
        var command = new CriarInsumoCommand("Farinha", "Ingrediente", 5m, "kg", 0m);

        var resultado = await _sut.CriarAsync(command);

        resultado.IsFailure.Should().BeTrue();
        _insumoRepositoryMock.Verify(x => x.AdicionarAsync(It.IsAny<Insumo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /* ===================== ATUALIZAÇÃO ===================== */

    [Fact]
    public async Task AtualizarAsync_DeveBuscarOInsumoNoEscopoDoUsuarioAutenticado()
    {
        var insumo = NovoInsumo();
        _insumoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(insumo.Id, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(insumo);

        var command = new AtualizarInsumoCommand(insumo.Id, "Farinha tipo 1", "Ingrediente", 10m, "kg", 48.50m);
        var resultado = await _sut.AtualizarAsync(command);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Name.Should().Be("Farinha tipo 1");
        resultado.Value.BaseQuantity.Should().Be(10000m);
        _insumoRepositoryMock.Verify(x => x.ObterPorIdAsync(insumo.Id, UsuarioId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_ComInsumoDeOutroUsuario_DeveFalhar()
    {
        // O repositório filtra por usuário; um id alheio simplesmente não é encontrado
        var idAlheio = Guid.NewGuid();
        _insumoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(idAlheio, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Insumo?)null);

        var command = new AtualizarInsumoCommand(idAlheio, "Farinha", "Ingrediente", 5m, "kg", 24.90m);
        var resultado = await _sut.AtualizarAsync(command);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("Insumo não encontrado.");
        _insumoRepositoryMock.Verify(x => x.AtualizarAsync(It.IsAny<Insumo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /* ===================== EXCLUSÃO ===================== */

    [Fact]
    public async Task ExcluirAsync_ComInsumoExistente_DeveRemover()
    {
        var insumo = NovoInsumo();
        _insumoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(insumo.Id, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(insumo);

        var resultado = await _sut.ExcluirAsync(new ExcluirInsumoCommand(insumo.Id));

        resultado.IsSuccess.Should().BeTrue();
        _insumoRepositoryMock.Verify(x => x.RemoverAsync(insumo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcluirAsync_ComInsumoInexistente_DeveFalhar()
    {
        _insumoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>(), UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Insumo?)null);

        var resultado = await _sut.ExcluirAsync(new ExcluirInsumoCommand(Guid.NewGuid()));

        resultado.IsFailure.Should().BeTrue();
        _insumoRepositoryMock.Verify(x => x.RemoverAsync(It.IsAny<Insumo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LimparAsync_DeveRemoverApenasOsInsumosDoUsuarioAutenticado()
    {
        var resultado = await _sut.LimparAsync(new LimparInsumosCommand());

        resultado.IsSuccess.Should().BeTrue();
        _insumoRepositoryMock.Verify(
            x => x.RemoverTodosPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
