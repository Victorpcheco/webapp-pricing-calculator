using System.ComponentModel.DataAnnotations;
using Application.Insumos.Commands;
using Application.Insumos.Queries;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Insumos;

/// <summary>
/// Exercita o MESMO validador que o MVC roda na requisição (IObjectModelValidator),
/// e não o Validator.TryValidateObject — que lê atributos de propriedade e por isso
/// passava verde enquanto a API quebrava em runtime com:
/// "Record type '...' has validation metadata defined on property '...'".
///
/// Em records posicionais o atributo precisa ficar no parâmetro do construtor.
/// </summary>
public class InsumoValidationTests
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

    private static CriarInsumoCommand CommandValido(
        string nome = "Farinha de trigo",
        string tipo = "Ingrediente",
        decimal quantidade = 5m,
        string unidade = "kg",
        decimal preco = 24.90m)
        => new(nome, tipo, quantidade, unidade, preco);

    /* ============ GUARDA DO PRÓPRIO HARNESS ============ */

    private record RecordQuebrado([property: Required] string Name);

    [Fact]
    public void Harness_DeveDetectarValidacaoDeclaradaNaPropriedade()
    {
        // Prova que os testes abaixo realmente pegariam a regressão — sem isso
        // eles poderiam virar no-op silencioso se o harness parasse de validar.
        var act = () => Validar(new RecordQuebrado("x"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*validation metadata defined on property*");
    }

    /* ============ QUERY DA LISTAGEM ============ */

    [Fact]
    public void ListarInsumosQuery_SemFiltros_DeveSerValida()
    {
        // Caminho mais comum da tela: GET /api/insumos sem query string
        Validar(new ListarInsumosQuery()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Ingrediente")]
    [InlineData("Embalagem")]
    public void ListarInsumosQuery_ComTipoConhecido_DeveSerValida(string tipo)
    {
        Validar(new ListarInsumosQuery(Tipo: tipo)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ListarInsumosQuery_ComTipoDesconhecido_DeveSerInvalida()
    {
        Validar(new ListarInsumosQuery(Tipo: "Bebida")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListarInsumosQuery_ComBuscaPorNome_DeveSerValida()
    {
        Validar(new ListarInsumosQuery(Nome: "farinha")).IsValid.Should().BeTrue();
    }

    /* ============ COMMAND DE CRIAÇÃO ============ */

    [Fact]
    public void CriarInsumoCommand_ComDadosValidos_DeveSerValido()
    {
        Validar(CommandValido()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("kg")]
    [InlineData("g")]
    [InlineData("L")]
    [InlineData("ml")]
    [InlineData("un")]
    public void CriarInsumoCommand_ComCadaUnidadeDoSelect_DeveSerValido(string unidade)
    {
        Validar(CommandValido(unidade: unidade)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("arroba")]
    [InlineData("KG")]
    [InlineData("")]
    public void CriarInsumoCommand_ComUnidadeForaDoSelect_DeveSerInvalido(string unidade)
    {
        Validar(CommandValido(unidade: unidade)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarInsumoCommand_SemNome_DeveSerInvalido()
    {
        Validar(CommandValido(nome: null!)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarInsumoCommand_ComNomeAcimaDe80Caracteres_DeveSerInvalido()
    {
        Validar(CommandValido(nome: new string('a', 81))).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.0009)]
    public void CriarInsumoCommand_ComQuantidadeAbaixoDoMinimo_DeveSerInvalido(decimal quantidade)
    {
        Validar(CommandValido(quantidade: quantidade)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarInsumoCommand_ComQuantidadeMinimaExata_DeveSerValido()
    {
        Validar(CommandValido(quantidade: 0.001m)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CriarInsumoCommand_ComPrecoNaoPositivo_DeveSerInvalido(decimal preco)
    {
        Validar(CommandValido(preco: preco)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarInsumoCommand_ComTipoForaDoRadio_DeveSerInvalido()
    {
        Validar(CommandValido(tipo: "Bebida")).IsValid.Should().BeFalse();
    }

    /* ============ COMMAND DE ATUALIZAÇÃO ============ */

    [Fact]
    public void AtualizarInsumoCommand_ComDadosValidos_DeveSerValido()
    {
        var command = new AtualizarInsumoCommand(
            Guid.NewGuid(), "Farinha de trigo", "Ingrediente", 5m, "kg", 24.90m);

        Validar(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void AtualizarInsumoCommand_ComUnidadeInvalida_DeveSerInvalido()
    {
        var command = new AtualizarInsumoCommand(
            Guid.NewGuid(), "Farinha de trigo", "Ingrediente", 5m, "arroba", 24.90m);

        Validar(command).IsValid.Should().BeFalse();
    }
}
