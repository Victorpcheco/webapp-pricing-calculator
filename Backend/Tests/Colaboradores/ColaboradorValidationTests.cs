using Application.Colaboradores.Commands;
using Application.Colaboradores.Queries;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Colaboradores;

/// <summary>
/// Exercita o MESMO validador que o MVC roda na requisição (IObjectModelValidator),
/// e não o Validator.TryValidateObject — que lê atributos de propriedade e por isso
/// passava verde enquanto a API quebrava em runtime com:
/// "Record type '...' has validation metadata defined on property '...'".
///
/// Em records posicionais o atributo precisa ficar no parâmetro do construtor.
/// </summary>
public class ColaboradorValidationTests
{
    private static readonly IServiceProvider Services = ConstruirServices();

    private static IServiceProvider ConstruirServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        return services.BuildServiceProvider();
    }

    /// <summary>Roda o pipeline de validação do MVC e devolve o ModelState resultante.</summary>
    private static ModelStateDictionary Validar(object modelo)
    {
        var validator = Services.GetRequiredService<IObjectModelValidator>();
        var context = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

        validator.Validate(context, new ValidationStateDictionary(), string.Empty, modelo);

        return context.ModelState;
    }

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

    [Fact]
    public void CriarColaboradorCommand_ComDadosValidos_DeveSerValido()
    {
        Validar(CommandValido()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CriarColaboradorCommand_SemCamposOpcionais_DeveSerValido()
    {
        Validar(CommandValido(codigo: null, telefone: null, admissao: null)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("CLT")]
    [InlineData("Freelancer")]
    public void CriarColaboradorCommand_ComCadaTipoDoRadio_DeveSerValido(string tipo)
    {
        Validar(CommandValido(tipo: tipo)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("PJ")]
    [InlineData("clt")]
    [InlineData("")]
    public void CriarColaboradorCommand_ComTipoForaDoRadio_DeveSerInvalido(string tipo)
    {
        Validar(CommandValido(tipo: tipo)).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Ativo")]
    [InlineData("Inativo")]
    public void CriarColaboradorCommand_ComCadaStatusDoSelect_DeveSerValido(string status)
    {
        Validar(CommandValido(status: status)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CriarColaboradorCommand_ComStatusForaDoSelect_DeveSerInvalido()
    {
        Validar(CommandValido(status: "Afastado")).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Mensal")]
    [InlineData("Por hora")]
    [InlineData("Por serviço")]
    [InlineData(null)]
    public void CriarColaboradorCommand_ComFormaDePagamentoDoSelect_DeveSerValido(string? frequencia)
    {
        Validar(CommandValido(tipo: "Freelancer", frequencia: frequencia)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Quinzenal")]
    [InlineData("por hora")]
    public void CriarColaboradorCommand_ComFormaDePagamentoForaDoSelect_DeveSerInvalido(string frequencia)
    {
        Validar(CommandValido(tipo: "Freelancer", frequencia: frequencia)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarColaboradorCommand_SemNome_DeveSerInvalido()
    {
        Validar(CommandValido(nome: null!)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarColaboradorCommand_ComNomeAcimaDe80Caracteres_DeveSerInvalido()
    {
        Validar(CommandValido(nome: new string('a', 81))).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarColaboradorCommand_SemCargo_DeveSerInvalido()
    {
        Validar(CommandValido(cargo: null!)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarColaboradorCommand_ComCargoAcimaDe60Caracteres_DeveSerInvalido()
    {
        Validar(CommandValido(cargo: new string('a', 61))).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarColaboradorCommand_ComCodigoAcimaDe30Caracteres_DeveSerInvalido()
    {
        Validar(CommandValido(codigo: new string('a', 31))).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarColaboradorCommand_ComTelefoneAcimaDe20Caracteres_DeveSerInvalido()
    {
        Validar(CommandValido(telefone: new string('9', 21))).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CriarColaboradorCommand_ComValorBaseNaoPositivo_DeveSerInvalido(decimal valorBase)
    {
        Validar(CommandValido(valorBase: valorBase)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AtualizarColaboradorCommand_ComDadosValidos_DeveSerValido()
    {
        var command = new AtualizarColaboradorCommand(
            Guid.NewGuid(), "COL-01", "Juliana Ferreira", "Confeiteira",
            "CLT", "Ativo", DateTime.UtcNow, 1900m, null, "(11) 98888-1234");

        Validar(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void AtualizarColaboradorCommand_ComValorBaseZero_DeveSerInvalido()
    {
        var command = new AtualizarColaboradorCommand(
            Guid.NewGuid(), null, "Juliana", "Confeiteira", "CLT", "Ativo", null, 0m, null, null);

        Validar(command).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("CLT")]
    [InlineData("Freelancer")]
    public void ListarColaboradoresQuery_ComTipoAceito_DeveSerValida(string? tipo)
    {
        Validar(new ListarColaboradoresQuery("juliana", tipo)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ListarColaboradoresQuery_ComTipoDesconhecido_DeveSerInvalida()
    {
        Validar(new ListarColaboradoresQuery(Tipo: "PJ")).IsValid.Should().BeFalse();
    }
}
