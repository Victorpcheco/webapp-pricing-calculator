using Application.Produtos.Commands;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Produtos;

/// <summary>
/// Mesmo pipeline que o MVC roda na requisição (IObjectModelValidator).
/// A prova de que esse harness detecta validação declarada na propriedade
/// — o bug que derrubava a API — está em Tests.Insumos.InsumoValidationTests.
/// </summary>
public class ProdutoValidationTests
{
    private static readonly IServiceProvider Services = ConstruirServices();

    private static IServiceProvider ConstruirServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        return services.BuildServiceProvider();
    }

    private static ModelStateDictionary Validar(object modelo)
    {
        var validator = Services.GetRequiredService<IObjectModelValidator>();
        var context = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

        validator.Validate(context, new ValidationStateDictionary(), string.Empty, modelo);

        return context.ModelState;
    }

    private static CriarProdutoCommand CommandValido(
        string nome = "Bolo de chocolate",
        string tipoProducao = "Porções",
        int rendimento = 10,
        string nomeUnidade = "fatia",
        int tempo = 60,
        IReadOnlyList<ItemComposicaoInput>? composicao = null)
        => new(nome, tipoProducao, rendimento, nomeUnidade, tempo,
            composicao ?? new[] { new ItemComposicaoInput(Guid.NewGuid(), 500m) });

    [Fact]
    public void CriarProdutoCommand_ComDadosValidos_DeveSerValido()
    {
        Validar(CommandValido()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CriarProdutoCommand_SemComposicao_DeveSerValido()
    {
        Validar(CommandValido(composicao: Array.Empty<ItemComposicaoInput>())).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Produto inteiro")]
    [InlineData("Porções")]
    public void CriarProdutoCommand_ComCadaTipoDoRadio_DeveSerValido(string tipo)
    {
        Validar(CommandValido(tipoProducao: tipo)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Lote")]
    [InlineData("porções")]
    [InlineData("")]
    public void CriarProdutoCommand_ComTipoForaDoRadio_DeveSerInvalido(string tipo)
    {
        Validar(CommandValido(tipoProducao: tipo)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarProdutoCommand_SemNome_DeveSerInvalido()
    {
        Validar(CommandValido(nome: null!)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarProdutoCommand_ComNomeAcimaDe80Caracteres_DeveSerInvalido()
    {
        Validar(CommandValido(nome: new string('a', 81))).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CriarProdutoCommand_ComRendimentoAbaixoDeUm_DeveSerInvalido(int rendimento)
    {
        Validar(CommandValido(rendimento: rendimento)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarProdutoCommand_SemNomeDaUnidade_DeveSerInvalido()
    {
        Validar(CommandValido(nomeUnidade: null!)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarProdutoCommand_ComTempoNegativo_DeveSerInvalido()
    {
        Validar(CommandValido(tempo: -1)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarProdutoCommand_ComTempoZero_DeveSerValido()
    {
        // Produto sem mão de obra é um caso legítimo
        Validar(CommandValido(tempo: 0)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ItemComposicao_ComQuantidadeNaoPositiva_DeveSerInvalido(decimal quantidade)
    {
        var command = CommandValido(composicao: new[] { new ItemComposicaoInput(Guid.NewGuid(), quantidade) });

        Validar(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AtualizarProdutoCommand_ComDadosValidos_DeveSerValido()
    {
        var command = new AtualizarProdutoCommand(
            Guid.NewGuid(), "Bolo", "Porções", 10, "fatia", 60,
            new[] { new ItemComposicaoInput(Guid.NewGuid(), 100m) });

        Validar(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void AtualizarProdutoCommand_ComRendimentoZero_DeveSerInvalido()
    {
        var command = new AtualizarProdutoCommand(
            Guid.NewGuid(), "Bolo", "Porções", 0, "fatia", 60, Array.Empty<ItemComposicaoInput>());

        Validar(command).IsValid.Should().BeFalse();
    }
}
