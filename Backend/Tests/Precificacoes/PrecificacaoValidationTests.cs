using Application.Precificacoes.Commands;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Precificacoes;

/// <summary>
/// Exercita o MESMO validador que o MVC roda na requisição (IObjectModelValidator),
/// e não o Validator.TryValidateObject — que lê atributos de propriedade e por isso
/// passava verde enquanto a API quebrava em runtime com:
/// "Record type '...' has validation metadata defined on property '...'".
///
/// Em records posicionais o atributo precisa ficar no parâmetro do construtor.
/// </summary>
public class PrecificacaoValidationTests
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

    private static CriarSimulacaoCommand CommandValido(
        Guid? recipeId = null,
        decimal margin = 40m,
        decimal salePrice = 3.9m,
        int quantity = 30)
        => new(recipeId ?? Guid.NewGuid(), margin, salePrice, quantity);

    [Fact]
    public void CriarSimulacaoCommand_ComDadosValidos_DeveSerValido()
    {
        Validar(CommandValido()).IsValid.Should().BeTrue();
    }

    // Guid.Empty não é pego pelo [Required] — RequiredAttribute só rejeita null, e um
    // tipo de valor não-anulável nunca é null. A guarda real fica no domínio
    // (SimulacaoPreco.Validar), coberta em SimulacaoPrecoTests.Criar_SemProduto_DeveFalhar.

    [Theory]
    [InlineData(0)]
    [InlineData(150)]
    [InlineData(1000)]
    public void CriarSimulacaoCommand_ComMargemDentroDoIntervalo_DeveSerValido(decimal margin)
    {
        Validar(CommandValido(margin: margin)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    public void CriarSimulacaoCommand_ComMargemForaDoIntervalo_DeveSerInvalido(decimal margin)
    {
        Validar(CommandValido(margin: margin)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarSimulacaoCommand_ComPrecoZero_DeveSerValido()
    {
        // Simular um preço abaixo do custo (inclusive zero) é um cenário legítimo
        Validar(CommandValido(salePrice: 0m)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CriarSimulacaoCommand_ComPrecoNegativo_DeveSerInvalido()
    {
        Validar(CommandValido(salePrice: -0.01m)).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CriarSimulacaoCommand_ComQuantidadeAbaixoDoMinimo_DeveSerInvalido(int quantity)
    {
        Validar(CommandValido(quantity: quantity)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarSimulacaoCommand_ComQuantidadeUm_DeveSerValido()
    {
        Validar(CommandValido(quantity: 1)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void AtualizarSimulacaoCommand_ComDadosValidos_DeveSerValido()
    {
        var command = new AtualizarSimulacaoCommand(Guid.NewGuid(), Guid.NewGuid(), 40m, 3.9m, 30);

        Validar(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void AtualizarSimulacaoCommand_ComQuantidadeZero_DeveSerInvalido()
    {
        var command = new AtualizarSimulacaoCommand(Guid.NewGuid(), Guid.NewGuid(), 40m, 3.9m, 0);

        Validar(command).IsValid.Should().BeFalse();
    }
}
