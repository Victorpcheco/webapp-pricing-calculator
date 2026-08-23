using Domain.Entities.Insumos;
using FluentAssertions;
using Insumo = Domain.Entities.Insumos.Insumo;

namespace Tests.Insumos;

public class InsumoTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private static Insumo CriarValido(
        decimal quantidade = 5m,
        UnidadeMedida unidade = UnidadeMedida.Quilograma,
        decimal preco = 24.90m,
        string nome = "Farinha de trigo",
        TipoInsumo tipo = TipoInsumo.Ingrediente)
    {
        var resultado = Insumo.Criar(UsuarioId, nome, tipo, quantidade, unidade, preco);
        resultado.IsSuccess.Should().BeTrue(resultado.Error);
        return resultado.Value;
    }

    /* ===================== CONVERSÃO DE UNIDADES ===================== */

    [Fact]
    public void Criar_ComQuilogramas_DeveConverterParaGramas()
    {
        var insumo = CriarValido(quantidade: 5m, unidade: UnidadeMedida.Quilograma, preco: 24.90m);

        insumo.QuantidadeBase.Should().Be(5000m);
        insumo.UnidadeBase.Should().Be(UnidadeMedida.Grama);
        insumo.PrecoUnitario.Should().Be(0.00498m);
    }

    [Fact]
    public void Criar_ComLitros_DeveConverterParaMililitros()
    {
        var insumo = CriarValido(quantidade: 2m, unidade: UnidadeMedida.Litro, preco: 12.00m);

        insumo.QuantidadeBase.Should().Be(2000m);
        insumo.UnidadeBase.Should().Be(UnidadeMedida.Mililitro);
        insumo.PrecoUnitario.Should().Be(0.006m);
    }

    [Theory]
    [InlineData(UnidadeMedida.Grama, UnidadeMedida.Grama)]
    [InlineData(UnidadeMedida.Mililitro, UnidadeMedida.Mililitro)]
    [InlineData(UnidadeMedida.Unidade, UnidadeMedida.Unidade)]
    public void Criar_ComUnidadeJaBase_NaoDeveAlterarAQuantidade(UnidadeMedida unidade, UnidadeMedida esperada)
    {
        var insumo = CriarValido(quantidade: 250m, unidade: unidade, preco: 50m);

        insumo.QuantidadeBase.Should().Be(250m);
        insumo.UnidadeBase.Should().Be(esperada);
        insumo.PrecoUnitario.Should().Be(0.2m);
    }

    [Fact]
    public void Criar_ComDivisaoNaoExata_DeveArredondarCustoEmSeisCasas()
    {
        // 10 ÷ 3 = 3,333... — sem as 6 casas o custo perderia precisão relevante
        var insumo = CriarValido(quantidade: 3m, unidade: UnidadeMedida.Unidade, preco: 10.00m);

        insumo.PrecoUnitario.Should().Be(3.333333m);
    }

    [Fact]
    public void Criar_ComInsumoBaratoPorGrama_NaoDeveZerarOCusto()
    {
        // Regra do frontend: abaixo de um centavo o custo ainda precisa ser representável
        var insumo = CriarValido(quantidade: 25m, unidade: UnidadeMedida.Quilograma, preco: 89.90m);

        insumo.PrecoUnitario.Should().BeGreaterThan(0m);
        insumo.PrecoUnitario.Should().Be(0.003596m);
    }

    /* ===================== VALIDAÇÕES ===================== */

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_SemNome_DeveFalhar(string? nome)
    {
        var resultado = Insumo.Criar(
            UsuarioId, nome, TipoInsumo.Ingrediente, 5m, UnidadeMedida.Quilograma, 24.90m);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O nome do insumo é obrigatório.");
    }

    [Fact]
    public void Criar_ComNomeAcimaDoLimite_DeveFalhar()
    {
        var nomeLongo = new string('a', Insumo.NomeTamanhoMaximo + 1);

        var resultado = Insumo.Criar(
            UsuarioId, nomeLongo, TipoInsumo.Ingrediente, 5m, UnidadeMedida.Quilograma, 24.90m);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("80 caracteres");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.0009)]
    public void Criar_ComQuantidadeAbaixoDoMinimo_DeveFalhar(decimal quantidade)
    {
        var resultado = Insumo.Criar(
            UsuarioId, "Farinha", TipoInsumo.Ingrediente, quantidade, UnidadeMedida.Quilograma, 24.90m);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("quantidade");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Criar_ComPrecoNaoPositivo_DeveFalhar(decimal preco)
    {
        var resultado = Insumo.Criar(
            UsuarioId, "Farinha", TipoInsumo.Ingrediente, 5m, UnidadeMedida.Quilograma, preco);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O preço total pago deve ser maior que zero.");
    }

    [Fact]
    public void Criar_ComQuantidadeMinimaExata_DeveSerAceito()
    {
        var resultado = Insumo.Criar(
            UsuarioId, "Corante", TipoInsumo.Ingrediente, 0.001m, UnidadeMedida.Quilograma, 5m);

        resultado.IsSuccess.Should().BeTrue();
    }

    /* ===================== NORMALIZAÇÃO ===================== */

    [Fact]
    public void Criar_ComEspacosNoNome_DeveArmazenarSemEspacosNasBordas()
    {
        var insumo = CriarValido(nome: "  Açúcar cristal  ");

        insumo.Nome.Should().Be("Açúcar cristal");
    }

    /* ===================== ATUALIZAÇÃO ===================== */

    [Fact]
    public void Atualizar_ComNovaUnidade_DeveRecalcularOsCamposDerivados()
    {
        var insumo = CriarValido(quantidade: 5m, unidade: UnidadeMedida.Quilograma, preco: 24.90m);

        var resultado = insumo.Atualizar(
            nome: "Farinha de trigo tipo 1",
            tipo: TipoInsumo.Ingrediente,
            quantidade: 500m,
            unidade: UnidadeMedida.Grama,
            preco: 4.00m);

        resultado.IsSuccess.Should().BeTrue();
        insumo.QuantidadeBase.Should().Be(500m);
        insumo.UnidadeBase.Should().Be(UnidadeMedida.Grama);
        insumo.PrecoUnitario.Should().Be(0.008m);
        insumo.Nome.Should().Be("Farinha de trigo tipo 1");
    }

    [Fact]
    public void Atualizar_DevePreservarCriadoEmEAvancarAtualizadoEm()
    {
        var insumo = CriarValido();
        var criadoOriginal = insumo.CriadoEm;
        var atualizadoOriginal = insumo.AtualizadoEm;

        insumo.Atualizar("Farinha", TipoInsumo.Ingrediente, 10m, UnidadeMedida.Quilograma, 48.50m);

        insumo.CriadoEm.Should().Be(criadoOriginal);
        insumo.AtualizadoEm.Should().BeOnOrAfter(atualizadoOriginal);
    }

    [Fact]
    public void Atualizar_ComDadosInvalidos_NaoDeveAlterarOEstado()
    {
        var insumo = CriarValido(quantidade: 5m, unidade: UnidadeMedida.Quilograma, preco: 24.90m);

        var resultado = insumo.Atualizar("", TipoInsumo.Ingrediente, 10m, UnidadeMedida.Grama, 5m);

        resultado.IsFailure.Should().BeTrue();
        insumo.Nome.Should().Be("Farinha de trigo");
        insumo.QuantidadeBase.Should().Be(5000m);
        insumo.PrecoUnitario.Should().Be(0.00498m);
    }

    [Fact]
    public void Atualizar_ParaEmbalagem_DeveTrocarOTipo()
    {
        var insumo = CriarValido(tipo: TipoInsumo.Ingrediente);

        insumo.Atualizar("Caixa 18x18", TipoInsumo.Embalagem, 50m, UnidadeMedida.Unidade, 75m);

        insumo.Tipo.Should().Be(TipoInsumo.Embalagem);
        insumo.PrecoUnitario.Should().Be(1.5m);
    }
}
