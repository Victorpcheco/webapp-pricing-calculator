using Application.Common;
using Application.Produtos.Commands;
using Application.Produtos.Queries;
using Application.Produtos.Services;
using Application.Repositories;
using Domain.Entities.Insumos;
using Domain.Entities.Produtos;
using FluentAssertions;
using Moq;

namespace Tests.Produtos;

public class ProdutoServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly Mock<IProdutoRepository> _produtoRepositoryMock = new();
    private readonly Mock<IInsumoRepository> _insumoRepositoryMock = new();
    private readonly Mock<ICustoRepository> _custoRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly ProdutoService _sut;

    private readonly Insumo _farinha;
    private readonly Insumo _caixa;

    public ProdutoServiceTests()
    {
        _currentUserMock.SetupGet(x => x.UsuarioId).Returns(UsuarioId);

        _farinha = Insumo.Criar(UsuarioId, "Farinha de trigo", TipoInsumo.Ingrediente,
            5m, UnidadeMedida.Quilograma, 24.90m).Value;          // 0,00498 / g
        _caixa = Insumo.Criar(UsuarioId, "Caixa para bolo", TipoInsumo.Embalagem,
            50m, UnidadeMedida.Unidade, 72.50m).Value;            // 1,45 / un

        _custoRepositoryMock
            .Setup(x => x.ObterValorHoraAtualAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(19.94m);

        _insumoRepositoryMock
            .Setup(x => x.ListarPorIdsAsync(UsuarioId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                new[] { _farinha, _caixa }.Where(i => ids.Contains(i.Id)).ToList());

        _sut = new ProdutoService(
            _produtoRepositoryMock.Object,
            _insumoRepositoryMock.Object,
            _custoRepositoryMock.Object,
            _currentUserMock.Object);
    }

    private Produto NovoProduto(int tempoMinutos = 60, int rendimento = 10)
        => Produto.Criar(UsuarioId, "Bolo de chocolate", TipoProducao.Porcoes, rendimento, "fatia", tempoMinutos,
            new[] { (_farinha.Id, 500m), (_caixa.Id, 1m) }).Value;

    private CriarProdutoCommand CommandValido(IReadOnlyList<ItemComposicaoInput>? composicao = null)
        => new("Bolo de chocolate", "Porções", 10, "fatia", 60,
            composicao ?? new[]
            {
                new ItemComposicaoInput(_farinha.Id, 500m),
                new ItemComposicaoInput(_caixa.Id, 1m)
            });

    /* ===================== LISTAGEM ===================== */

    [Fact]
    public async Task ListarAsync_DeveDevolverAFichaComOsCustosCalculados()
    {
        _produtoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { NovoProduto() });

        var resultado = await _sut.ListarAsync(new ListarProdutosQuery());

        resultado.IsSuccess.Should().BeTrue();
        var ficha = resultado.Value.Data.Single();

        ficha.ProductionType.Should().Be("Porções");
        ficha.MaterialsCost.Should().Be(3.94m);   // 2,49 + 1,45
        ficha.LaborCost.Should().Be(19.94m);
        ficha.TotalCost.Should().Be(23.88m);
        ficha.UnitCost.Should().Be(2.388m);
        ficha.HourlyRateUsed.Should().Be(19.94m);
    }

    [Fact]
    public async Task ListarAsync_DeveResolverNomeEUnidadeBaseDeCadaLinha()
    {
        _produtoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { NovoProduto() });

        var resultado = await _sut.ListarAsync(new ListarProdutosQuery());

        var linha = resultado.Value.Data.Single().Composition.Single(c => c.SupplyId == _farinha.Id);
        linha.SupplyName.Should().Be("Farinha de trigo");
        linha.SupplyAvailable.Should().BeTrue();
        linha.BaseUnit.Should().Be("g");
        linha.SupplyUnitCost.Should().Be(0.00498m);
        linha.Cost.Should().Be(2.49m);
    }

    [Fact]
    public async Task ListarAsync_ComInsumoExcluido_DeveMarcarALinhaComoIndisponivel()
    {
        var idFantasma = Guid.NewGuid();
        var produto = Produto.Criar(UsuarioId, "Bolo", TipoProducao.Porcoes, 10, "fatia", 0,
            new[] { (idFantasma, 300m) }).Value;

        _produtoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { produto });

        var resultado = await _sut.ListarAsync(new ListarProdutosQuery());

        var linha = resultado.Value.Data.Single().Composition.Single();
        linha.SupplyAvailable.Should().BeFalse();
        linha.SupplyName.Should().BeNull();
        linha.Cost.Should().Be(0m);
    }

    [Fact]
    public async Task ListarAsync_SemConfiguracaoDeCusto_DeveUsarValorHoraZero()
    {
        _custoRepositoryMock
            .Setup(x => x.ObterValorHoraAtualAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
        _produtoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { NovoProduto() });

        var resultado = await _sut.ListarAsync(new ListarProdutosQuery());

        resultado.Value.Meta.HourlyRate.Should().Be(0m);
        resultado.Value.Data.Single().LaborCost.Should().Be(0m);
    }

    [Fact]
    public async Task ListarAsync_DeveRepassarABuscaPorNome()
    {
        _produtoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, "bolo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Produto>());

        await _sut.ListarAsync(new ListarProdutosQuery("bolo"));

        _produtoRepositoryMock.Verify(
            x => x.ListarPorUsuarioAsync(UsuarioId, "bolo", It.IsAny<CancellationToken>()), Times.Once);
    }

    /* ===================== CRIAÇÃO ===================== */

    [Fact]
    public async Task CriarAsync_ComDadosValidos_DevePersistirNoEscopoDoUsuario()
    {
        var resultado = await _sut.CriarAsync(CommandValido());

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.TotalCost.Should().Be(23.88m);

        _produtoRepositoryMock.Verify(x => x.AdicionarAsync(
            It.Is<Produto>(p => p.UsuarioId == UsuarioId && p.Composicao.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_ComInsumoInexistente_DeveFalharSemPersistir()
    {
        var command = CommandValido(new[] { new ItemComposicaoInput(Guid.NewGuid(), 100m) });

        var resultado = await _sut.CriarAsync(command);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("não existe no seu cadastro");
        _produtoRepositoryMock.Verify(
            x => x.AdicionarAsync(It.IsAny<Produto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_ComTipoDeProducaoInvalido_DeveFalhar()
    {
        var command = CommandValido() with { ProductionType = "Lote" };

        var resultado = await _sut.CriarAsync(command);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("tipo de produção");
    }

    [Fact]
    public async Task CriarAsync_ComInsumoRepetido_DeveFalhar()
    {
        var command = CommandValido(new[]
        {
            new ItemComposicaoInput(_farinha.Id, 200m),
            new ItemComposicaoInput(_farinha.Id, 300m)
        });

        var resultado = await _sut.CriarAsync(command);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("mais de uma vez");
    }

    [Fact]
    public async Task CriarAsync_SemComposicao_DeveSerAceito()
    {
        var command = CommandValido(Array.Empty<ItemComposicaoInput>());

        var resultado = await _sut.CriarAsync(command);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.MaterialsCost.Should().Be(0m);
        resultado.Value.Composition.Should().BeEmpty();
    }

    /* ===================== ATUALIZAÇÃO ===================== */

    [Fact]
    public async Task AtualizarAsync_DeveBuscarNoEscopoDoUsuarioERecalcular()
    {
        var produto = NovoProduto();
        _produtoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(produto.Id, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(produto);

        var command = new AtualizarProdutoCommand(
            produto.Id, "Bolo simples", "Produto inteiro", 1, "bolo", 0,
            new[] { new ItemComposicaoInput(_caixa.Id, 2m) });

        var resultado = await _sut.AtualizarAsync(command);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.ProductionType.Should().Be("Produto inteiro");
        resultado.Value.MaterialsCost.Should().Be(2.90m);
        resultado.Value.LaborCost.Should().Be(0m);
        resultado.Value.Composition.Should().HaveCount(1);
    }

    [Fact]
    public async Task AtualizarAsync_ComProdutoDeOutroUsuario_DeveFalhar()
    {
        var idAlheio = Guid.NewGuid();
        _produtoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(idAlheio, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Produto?)null);

        var command = new AtualizarProdutoCommand(
            idAlheio, "Bolo", "Porções", 10, "fatia", 60, Array.Empty<ItemComposicaoInput>());

        var resultado = await _sut.AtualizarAsync(command);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("Produto não encontrado.");
        _produtoRepositoryMock.Verify(
            x => x.AtualizarAsync(It.IsAny<Produto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /* ===================== EXCLUSÃO ===================== */

    [Fact]
    public async Task ExcluirAsync_ComProdutoExistente_DeveRemover()
    {
        var produto = NovoProduto();
        _produtoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(produto.Id, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(produto);

        var resultado = await _sut.ExcluirAsync(new ExcluirProdutoCommand(produto.Id));

        resultado.IsSuccess.Should().BeTrue();
        _produtoRepositoryMock.Verify(x => x.RemoverAsync(produto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcluirAsync_ComProdutoInexistente_DeveFalhar()
    {
        _produtoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>(), UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Produto?)null);

        var resultado = await _sut.ExcluirAsync(new ExcluirProdutoCommand(Guid.NewGuid()));

        resultado.IsFailure.Should().BeTrue();
        _produtoRepositoryMock.Verify(
            x => x.RemoverAsync(It.IsAny<Produto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LimparAsync_DeveRemoverApenasDoUsuarioAutenticado()
    {
        var resultado = await _sut.LimparAsync(new LimparProdutosCommand());

        resultado.IsSuccess.Should().BeTrue();
        _produtoRepositoryMock.Verify(
            x => x.RemoverTodosPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
