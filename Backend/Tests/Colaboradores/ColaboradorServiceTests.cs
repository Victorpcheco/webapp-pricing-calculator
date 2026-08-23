using Application.Colaboradores.Commands;
using Application.Colaboradores.Queries;
using Application.Colaboradores.Services;
using Application.Common;
using Application.Repositories;
using Domain.Entities.Colaboradores;
using FluentAssertions;
using Moq;

namespace Tests.Colaboradores;

public class ColaboradorServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly Mock<IColaboradorRepository> _colaboradorRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly ColaboradorService _sut;

    public ColaboradorServiceTests()
    {
        _colaboradorRepositoryMock = new Mock<IColaboradorRepository>();
        _currentUserMock = new Mock<ICurrentUserService>();
        _currentUserMock.SetupGet(x => x.UsuarioId).Returns(UsuarioId);

        _colaboradorRepositoryMock
            .Setup(x => x.ObterResumoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ColaboradoresResumo.Vazio);

        _sut = new ColaboradorService(_colaboradorRepositoryMock.Object, _currentUserMock.Object);
    }

    private static Colaborador NovoClt(decimal salario = 1900m)
        => Colaborador.Criar(
            UsuarioId, "COL-01", "Juliana Ferreira", "Confeiteira",
            TipoContratacao.Clt, StatusColaborador.Ativo, null, salario, null, "(11) 98888-1234").Value;

    private static Colaborador NovoFreelancer(FrequenciaFreelancer frequencia = FrequenciaFreelancer.PorHora)
        => Colaborador.Criar(
            UsuarioId, null, "Rafael Souza", "Designer de embalagens",
            TipoContratacao.Freelancer, StatusColaborador.Ativo, null, 45m, frequencia, null).Value;

    private static CriarColaboradorCommand CommandValido(
        string? codigo = "COL-01",
        string nome = "Juliana Ferreira",
        string cargo = "Confeiteira",
        string tipo = "CLT",
        string status = "Ativo",
        DateTime? admissao = null,
        decimal valorBase = 1900m,
        string? frequencia = null,
        string? telefone = "(11) 98888-1234")
        => new(codigo, nome, cargo, tipo, status, admissao, valorBase, frequencia, telefone);

    /* ===================== LISTAGEM ===================== */

    [Fact]
    public async Task ListarAsync_DeveMapearOsItensComOsTokensEsperadosPeloFrontend()
    {
        _colaboradorRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { NovoClt() });

        var resultado = await _sut.ListarAsync(new ListarColaboradoresQuery());

        resultado.IsSuccess.Should().BeTrue();
        var item = resultado.Value.Data.Single();
        item.ContractType.Should().Be("CLT");
        item.Status.Should().Be("Ativo");
        item.FreelancerFrequency.Should().BeNull();
        item.Charges.Total.Should().Be(521.4444m);
        item.MonthlyCost.Should().Be(2421.4444m);
    }

    [Fact]
    public async Task ListarAsync_DoFreelancer_DeveDevolverEncargosZeradosEAFrequencia()
    {
        _colaboradorRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { NovoFreelancer() });

        var item = (await _sut.ListarAsync(new ListarColaboradoresQuery())).Value.Data.Single();

        item.ContractType.Should().Be("Freelancer");
        item.FreelancerFrequency.Should().Be("Por hora");
        item.Charges.Total.Should().Be(0m);
        item.MonthlyCost.Should().Be(0m);
    }

    [Fact]
    public async Task ListarAsync_DeveRepassarOsFiltrosDaToolbarParaORepositorio()
    {
        _colaboradorRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, "juliana", TipoContratacao.Clt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Colaborador>());

        var resultado = await _sut.ListarAsync(new ListarColaboradoresQuery("juliana", "CLT"));

        resultado.IsSuccess.Should().BeTrue();
        _colaboradorRepositoryMock.Verify(
            x => x.ListarPorUsuarioAsync(UsuarioId, "juliana", TipoContratacao.Clt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListarAsync_ComTipoDesconhecido_DeveFalhar()
    {
        var resultado = await _sut.ListarAsync(new ListarColaboradoresQuery(Tipo: "PJ"));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O tipo de contratação deve ser 'CLT' ou 'Freelancer'.");
    }

    [Fact]
    public async Task ListarAsync_DeveDevolverOResumoGlobalDoUsuario()
    {
        _colaboradorRepositoryMock
            .Setup(x => x.ListarPorUsuarioAsync(UsuarioId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Colaborador>());
        _colaboradorRepositoryMock
            .Setup(x => x.ObterResumoAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ColaboradoresResumo(3, 2, 1, 5321.44m));

        var resultado = await _sut.ListarAsync(new ListarColaboradoresQuery());

        resultado.Value.Meta.Should().Be(new ColaboradoresResumo(3, 2, 1, 5321.44m));
    }

    /* ===================== CRIAÇÃO ===================== */

    [Fact]
    public async Task CriarAsync_DevePersistirEDevolverOColaboradorCalculado()
    {
        var resultado = await _sut.CriarAsync(CommandValido());

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Name.Should().Be("Juliana Ferreira");
        resultado.Value.MonthlyCost.Should().Be(2421.4444m);
        _colaboradorRepositoryMock.Verify(
            x => x.AdicionarAsync(It.IsAny<Colaborador>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_DeFreelancer_DeveGuardarAFormaDePagamento()
    {
        var resultado = await _sut.CriarAsync(
            CommandValido(tipo: "Freelancer", valorBase: 45m, frequencia: "Por serviço"));

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.FreelancerFrequency.Should().Be("Por serviço");
    }

    [Fact]
    public async Task CriarAsync_ComTipoDeContratacaoInvalido_NaoDeveTocarNoRepositorio()
    {
        var resultado = await _sut.CriarAsync(CommandValido(tipo: "PJ"));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O tipo de contratação deve ser 'CLT' ou 'Freelancer'.");
        _colaboradorRepositoryMock.Verify(
            x => x.AdicionarAsync(It.IsAny<Colaborador>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_ComStatusInvalido_DeveFalhar()
    {
        var resultado = await _sut.CriarAsync(CommandValido(status: "Afastado"));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O status deve ser 'Ativo' ou 'Inativo'.");
    }

    [Fact]
    public async Task CriarAsync_ComFormaDePagamentoInvalida_DeveFalhar()
    {
        var resultado = await _sut.CriarAsync(CommandValido(tipo: "Freelancer", frequencia: "Quinzenal"));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("A forma de pagamento deve ser 'Mensal', 'Por hora' ou 'Por serviço'.");
    }

    [Fact]
    public async Task CriarAsync_ComValorBaseZerado_DevePropagarOErroDoDominio()
    {
        var resultado = await _sut.CriarAsync(CommandValido(valorBase: 0m));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O valor base deve ser maior que zero.");
    }

    /* ===================== ATUALIZAÇÃO ===================== */

    [Fact]
    public async Task AtualizarAsync_DeveReprovisionarOsEncargosComOSalarioNovo()
    {
        var colaborador = NovoClt();
        _colaboradorRepositoryMock
            .Setup(x => x.ObterPorIdAsync(colaborador.Id, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(colaborador);

        var command = new AtualizarColaboradorCommand(
            colaborador.Id, "COL-01", "Juliana Ferreira", "Confeiteira chefe",
            "CLT", "Ativo", null, 2400m, null, null);

        var resultado = await _sut.AtualizarAsync(command);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Role.Should().Be("Confeiteira chefe");
        resultado.Value.Charges.Fgts.Should().Be(192m);
        resultado.Value.MonthlyCost.Should().Be(3058.6667m);
        _colaboradorRepositoryMock.Verify(
            x => x.AtualizarAsync(colaborador, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_DeColaboradorInexistente_DeveFalhar()
    {
        _colaboradorRepositoryMock
            .Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>(), UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Colaborador?)null);

        var command = new AtualizarColaboradorCommand(
            Guid.NewGuid(), null, "Juliana", "Confeiteira", "CLT", "Ativo", null, 1900m, null, null);

        var resultado = await _sut.AtualizarAsync(command);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("Colaborador não encontrado.");
    }

    /* ===================== EXCLUSÃO ===================== */

    [Fact]
    public async Task ExcluirAsync_DeveRemoverOColaboradorDoUsuario()
    {
        var colaborador = NovoClt();
        _colaboradorRepositoryMock
            .Setup(x => x.ObterPorIdAsync(colaborador.Id, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(colaborador);

        var resultado = await _sut.ExcluirAsync(new ExcluirColaboradorCommand(colaborador.Id));

        resultado.IsSuccess.Should().BeTrue();
        _colaboradorRepositoryMock.Verify(
            x => x.RemoverAsync(colaborador, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcluirAsync_DeColaboradorDeOutroUsuario_DeveFalhar()
    {
        _colaboradorRepositoryMock
            .Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>(), UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Colaborador?)null);

        var resultado = await _sut.ExcluirAsync(new ExcluirColaboradorCommand(Guid.NewGuid()));

        resultado.IsFailure.Should().BeTrue();
        _colaboradorRepositoryMock.Verify(
            x => x.RemoverAsync(It.IsAny<Colaborador>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LimparAsync_DeveApagarApenasOsColaboradoresDoUsuarioAutenticado()
    {
        var resultado = await _sut.LimparAsync(new LimparColaboradoresCommand());

        resultado.IsSuccess.Should().BeTrue();
        _colaboradorRepositoryMock.Verify(
            x => x.RemoverTodosPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
