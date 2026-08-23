using Application.Common;
using Application.Custos.Commands;
using Application.Custos.Queries;
using Application.Custos.Services;
using Application.Repositories;
using Domain.Entities.Custos;
using FluentAssertions;
using Moq;

namespace Tests.Custos;

public class CustoServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly Mock<ICustoRepository> _custoRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly CustoService _sut;

    public CustoServiceTests()
    {
        _currentUserMock.SetupGet(x => x.UsuarioId).Returns(UsuarioId);
        _sut = new CustoService(_custoRepositoryMock.Object, _currentUserMock.Object);
    }

    private static CustoOperacional NovoCusto(string descricao = "Configuração atual")
        => CustoOperacional.Criar(
            UsuarioId, descricao, 3000m, 176, 450m, 30m, 180m, 70m, true, 80.90m, 5m).Value;

    private static CriarCustoCommand CommandValido(decimal proLabore = 3000m, int horas = 176)
        => new("Configuração atual", proLabore, horas, 450m, 30m, 180m, 70m, true, 80.90m, 5m);

    /* ===================== LISTAGEM ===================== */

    [Fact]
    public async Task ListarAsync_DeveBuscarApenasNoEscopoDoUsuarioAutenticado()
    {
        _custoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { NovoCusto() });

        var resultado = await _sut.ListarAsync(new ListarCustosQuery());

        resultado.Should().HaveCount(1);
        _custoRepositoryMock.Verify(
            x => x.ListarPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarAsync_DeveMapearOsCamposCalculadosParaOResult()
    {
        _custoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { NovoCusto("Configuração de verão") });

        var item = (await _sut.ListarAsync(new ListarCustosQuery())).Single();

        item.Description.Should().Be("Configuração de verão");
        item.Salary.Should().Be(3000m);
        item.Hours.Should().Be(176);
        item.EnergyReal.Should().Be(135m);
        item.GasReal.Should().Be(126m);
        item.Depreciation.Should().Be(167.0950m);
        item.Monthly.Should().Be(3508.9950m);
        item.Hour.Should().BeApproximately(19.9375m, 0.0001m);
    }

    [Fact]
    public async Task ListarAsync_SemHistorico_DeveRetornarListaVazia()
    {
        _custoRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CustoOperacional>());

        (await _sut.ListarAsync(new ListarCustosQuery())).Should().BeEmpty();
    }

    /* ===================== CRIAÇÃO ===================== */

    [Fact]
    public async Task CriarAsync_ComDadosValidos_DevePersistirNoEscopoDoUsuario()
    {
        var resultado = await _sut.CriarAsync(CommandValido());

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Hour.Should().BeApproximately(19.9375m, 0.0001m);

        _custoRepositoryMock.Verify(x => x.AdicionarAsync(
            It.Is<CustoOperacional>(c => c.UsuarioId == UsuarioId && c.ProLabore == 3000m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_ComProLaboreZero_DeveFalharSemPersistir()
    {
        var resultado = await _sut.CriarAsync(CommandValido(proLabore: 0m));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O pró-labore deve ser maior que zero.");
        _custoRepositoryMock.Verify(
            x => x.AdicionarAsync(It.IsAny<CustoOperacional>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_ComHorasForaDoIntervalo_DeveFalharSemPersistir()
    {
        var resultado = await _sut.CriarAsync(CommandValido(horas: 800));

        resultado.IsFailure.Should().BeTrue();
        _custoRepositoryMock.Verify(
            x => x.AdicionarAsync(It.IsAny<CustoOperacional>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /* ===================== ATUALIZAÇÃO ===================== */

    [Fact]
    public async Task AtualizarAsync_DeveBuscarNoEscopoDoUsuarioERecalcular()
    {
        var custo = NovoCusto();
        _custoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(custo.Id, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(custo);

        var command = new AtualizarCustoCommand(
            custo.Id, "ignorado", 4000m, 200, 500m, 50m, 200m, 50m, false, 80.90m, 0m);

        var resultado = await _sut.AtualizarAsync(command);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Monthly.Should().Be(4350m);
        resultado.Value.Hour.Should().Be(21.75m);
        _custoRepositoryMock.Verify(
            x => x.ObterPorIdAsync(custo.Id, UsuarioId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_ComCustoDeOutroUsuario_DeveFalhar()
    {
        var idAlheio = Guid.NewGuid();
        _custoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(idAlheio, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustoOperacional?)null);

        var command = new AtualizarCustoCommand(
            idAlheio, null, 3000m, 176, 450m, 30m, 180m, 70m, true, 80.90m, 5m);

        var resultado = await _sut.AtualizarAsync(command);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("Configuração de custo não encontrada.");
        _custoRepositoryMock.Verify(
            x => x.AtualizarAsync(It.IsAny<CustoOperacional>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_ComValoresInvalidos_NaoDevePersistir()
    {
        var custo = NovoCusto();
        _custoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(custo.Id, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(custo);

        var command = new AtualizarCustoCommand(
            custo.Id, null, 0m, 176, 450m, 30m, 180m, 70m, true, 80.90m, 5m);

        var resultado = await _sut.AtualizarAsync(command);

        resultado.IsFailure.Should().BeTrue();
        _custoRepositoryMock.Verify(
            x => x.AtualizarAsync(It.IsAny<CustoOperacional>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_NaoDeveTrocarADescricaoExistente()
    {
        // O command carrega Description, mas a entidade não expõe isso no Atualizar
        var custo = NovoCusto("Configuração de verão");
        _custoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(custo.Id, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(custo);

        var command = new AtualizarCustoCommand(
            custo.Id, "Nome novo", 4000m, 200, 500m, 50m, 200m, 50m, false, 0m, 0m);

        var resultado = await _sut.AtualizarAsync(command);

        resultado.Value.Description.Should().Be("Configuração de verão");
    }

    /* ===================== EXCLUSÃO ===================== */

    [Fact]
    public async Task ExcluirAsync_ComCustoExistente_DeveRemover()
    {
        var custo = NovoCusto();
        _custoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(custo.Id, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(custo);

        var resultado = await _sut.ExcluirAsync(new ExcluirCustoCommand(custo.Id));

        resultado.IsSuccess.Should().BeTrue();
        _custoRepositoryMock.Verify(x => x.RemoverAsync(custo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcluirAsync_ComCustoInexistente_DeveFalhar()
    {
        _custoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>(), UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustoOperacional?)null);

        var resultado = await _sut.ExcluirAsync(new ExcluirCustoCommand(Guid.NewGuid()));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("Configuração de custo não encontrada.");
        _custoRepositoryMock.Verify(
            x => x.RemoverAsync(It.IsAny<CustoOperacional>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LimparHistoricoAsync_DeveRemoverApenasDoUsuarioAutenticado()
    {
        var resultado = await _sut.LimparHistoricoAsync(new LimparHistoricoCustosCommand());

        resultado.IsSuccess.Should().BeTrue();
        _custoRepositoryMock.Verify(
            x => x.RemoverTodosPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
