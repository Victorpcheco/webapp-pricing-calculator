using Domain.Entities.Produtos;
using FluentAssertions;

namespace Tests.Produtos;

public class ProdutoTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid Farinha = Guid.NewGuid();
    private static readonly Guid Leite = Guid.NewGuid();
    private static readonly Guid Caixa = Guid.NewGuid();

    private static Produto CriarValido(
        string nome = "Bolo de chocolate",
        int rendimento = 10,
        string nomeUnidade = "fatia",
        int tempoMinutos = 60,
        IEnumerable<(Guid, decimal)>? composicao = null,
        TipoProducao tipo = TipoProducao.Porcoes)
    {
        var resultado = Produto.Criar(
            UsuarioId, nome, tipo, rendimento, nomeUnidade, tempoMinutos,
            composicao ?? new[] { (Farinha, 500m), (Leite, 250m), (Caixa, 1m) });

        resultado.IsSuccess.Should().BeTrue(resultado.Error);
        return resultado.Value;
    }

    /// <summary>Custos por unidade base: farinha R$/g, leite R$/ml, caixa R$/un.</summary>
    private static Dictionary<Guid, decimal> Custos() => new()
    {
        [Farinha] = 0.00498m,
        [Leite] = 0.00579m,
        [Caixa] = 1.45m
    };

    /* ===================== CÁLCULO ===================== */

    [Fact]
    public void Calcular_ComComposicaoEValorHora_DeveSomarMateriaisETrabalho()
    {
        var produto = CriarValido(tempoMinutos: 60, rendimento: 10);

        var ficha = produto.Calcular(Custos(), valorHora: 19.94m);

        // 500 × 0,00498 = 2,49 | 250 × 0,00579 = 1,4475 | 1 × 1,45 = 1,45
        ficha.CustoMateriais.Should().Be(5.3875m);
        ficha.CustoTrabalho.Should().Be(19.94m);
        ficha.CustoTotal.Should().Be(25.3275m);
        ficha.CustoUnitario.Should().Be(2.5328m);
    }

    [Fact]
    public void Calcular_DeveDetalharOCustoDeCadaLinha()
    {
        var produto = CriarValido();

        var ficha = produto.Calcular(Custos(), valorHora: 0m);

        ficha.Linhas.Should().HaveCount(3);
        ficha.Linhas.Single(l => l.InsumoId == Farinha).Custo.Should().Be(2.49m);
        ficha.Linhas.Single(l => l.InsumoId == Leite).Custo.Should().Be(1.4475m);
        ficha.Linhas.Single(l => l.InsumoId == Caixa).Custo.Should().Be(1.45m);
    }

    [Fact]
    public void Calcular_ComInsumoExcluido_DeveManterALinhaComCustoZero()
    {
        // A ficha não pode sumir porque um insumo foi apagado depois
        var produto = CriarValido();
        var custosSemLeite = new Dictionary<Guid, decimal>
        {
            [Farinha] = 0.00498m,
            [Caixa] = 1.45m
        };

        var ficha = produto.Calcular(custosSemLeite, valorHora: 0m);

        var linhaLeite = ficha.Linhas.Single(l => l.InsumoId == Leite);
        linhaLeite.Custo.Should().Be(0m);
        linhaLeite.CustoUnitarioInsumo.Should().Be(0m);
        ficha.CustoMateriais.Should().Be(3.94m);
    }

    [Fact]
    public void Calcular_SemValorHora_NaoDeveCobrarTrabalho()
    {
        var produto = CriarValido(tempoMinutos: 90);

        var ficha = produto.Calcular(Custos(), valorHora: 0m);

        ficha.CustoTrabalho.Should().Be(0m);
        ficha.CustoTotal.Should().Be(ficha.CustoMateriais);
    }

    [Fact]
    public void Calcular_ComTempoZero_NaoDeveCobrarTrabalho()
    {
        var produto = CriarValido(tempoMinutos: 0);

        var ficha = produto.Calcular(Custos(), valorHora: 50m);

        ficha.CustoTrabalho.Should().Be(0m);
    }

    [Theory]
    [InlineData(30, 20, 10)]
    [InlineData(90, 20, 30)]
    [InlineData(45, 40, 30)]
    public void Calcular_DeveConverterMinutosEmFracaoDeHora(int minutos, decimal valorHora, decimal esperado)
    {
        var produto = CriarValido(tempoMinutos: minutos, composicao: Array.Empty<(Guid, decimal)>());

        var ficha = produto.Calcular(Custos(), valorHora);

        ficha.CustoTrabalho.Should().Be(esperado);
    }

    [Fact]
    public void Calcular_SemComposicao_DeveTerCustoDeMateriaisZero()
    {
        var produto = CriarValido(composicao: Array.Empty<(Guid, decimal)>(), tempoMinutos: 0);

        var ficha = produto.Calcular(Custos(), valorHora: 19.94m);

        ficha.CustoMateriais.Should().Be(0m);
        ficha.Linhas.Should().BeEmpty();
    }

    [Fact]
    public void Calcular_DeveDividirOTotalPeloRendimento()
    {
        var produto = CriarValido(rendimento: 4, tempoMinutos: 0,
            composicao: new[] { (Caixa, 8m) }); // 8 × 1,45 = 11,60

        var ficha = produto.Calcular(Custos(), valorHora: 0m);

        ficha.CustoTotal.Should().Be(11.60m);
        ficha.CustoUnitario.Should().Be(2.90m);
    }

    [Fact]
    public void Calcular_ComDivisaoNaoExata_DeveArredondarEmQuatroCasas()
    {
        var produto = CriarValido(rendimento: 3, tempoMinutos: 0,
            composicao: new[] { (Caixa, 7m) }); // 10,15 ÷ 3 = 3,38333...

        var ficha = produto.Calcular(Custos(), valorHora: 0m);

        ficha.CustoUnitario.Should().Be(3.3833m);
    }

    /* ===================== VALIDAÇÕES ===================== */

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_SemNome_DeveFalhar(string? nome)
    {
        var resultado = Produto.Criar(
            UsuarioId, nome, TipoProducao.Porcoes, 10, "fatia", 60, Array.Empty<(Guid, decimal)>());

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O nome do produto é obrigatório.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Criar_ComRendimentoAbaixoDeUm_DeveFalhar(int rendimento)
    {
        var resultado = Produto.Criar(
            UsuarioId, "Bolo", TipoProducao.Porcoes, rendimento, "fatia", 60, Array.Empty<(Guid, decimal)>());

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("rendimento");
    }

    [Fact]
    public void Criar_SemNomeDaUnidade_DeveFalhar()
    {
        var resultado = Produto.Criar(
            UsuarioId, "Bolo", TipoProducao.Porcoes, 10, "  ", 60, Array.Empty<(Guid, decimal)>());

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("O nome da unidade é obrigatório.");
    }

    [Fact]
    public void Criar_ComTempoNegativo_DeveFalhar()
    {
        var resultado = Produto.Criar(
            UsuarioId, "Bolo", TipoProducao.Porcoes, 10, "fatia", -1, Array.Empty<(Guid, decimal)>());

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("tempo");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Criar_ComQuantidadeNaoPositivaNaComposicao_DeveFalhar(decimal quantidade)
    {
        var resultado = Produto.Criar(
            UsuarioId, "Bolo", TipoProducao.Porcoes, 10, "fatia", 60,
            new[] { (Farinha, quantidade) });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("quantidade");
    }

    [Fact]
    public void Criar_ComInsumoRepetidoNaComposicao_DeveFalhar()
    {
        var resultado = Produto.Criar(
            UsuarioId, "Bolo", TipoProducao.Porcoes, 10, "fatia", 60,
            new[] { (Farinha, 200m), (Leite, 100m), (Farinha, 300m) });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("mais de uma vez");
    }

    [Fact]
    public void Criar_ComInsumoVazioNaComposicao_DeveFalhar()
    {
        var resultado = Produto.Criar(
            UsuarioId, "Bolo", TipoProducao.Porcoes, 10, "fatia", 60,
            new[] { (Guid.Empty, 200m) });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("sem insumo");
    }

    [Fact]
    public void Criar_SemComposicao_DeveSerAceito()
    {
        // Produto pode ser cadastrado antes de montar a ficha
        var resultado = Produto.Criar(
            UsuarioId, "Bolo", TipoProducao.Porcoes, 10, "fatia", 60, Array.Empty<(Guid, decimal)>());

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Composicao.Should().BeEmpty();
    }

    /* ===================== ATUALIZAÇÃO ===================== */

    [Fact]
    public void Atualizar_DeveSubstituirAComposicaoPorInteiro()
    {
        var produto = CriarValido();
        produto.Composicao.Should().HaveCount(3);

        var resultado = produto.Atualizar(
            "Bolo simples", TipoProducao.ProdutoInteiro, 1, "bolo", 30,
            new[] { (Farinha, 800m) });

        resultado.IsSuccess.Should().BeTrue();
        produto.Composicao.Should().HaveCount(1);
        produto.Composicao.Single().InsumoId.Should().Be(Farinha);
        produto.Composicao.Single().Quantidade.Should().Be(800m);
        produto.TipoProducao.Should().Be(TipoProducao.ProdutoInteiro);
    }

    [Fact]
    public void Atualizar_ComComposicaoVazia_DeveEsvaziarAFicha()
    {
        var produto = CriarValido();

        produto.Atualizar("Bolo", TipoProducao.Porcoes, 10, "fatia", 60, Array.Empty<(Guid, decimal)>());

        produto.Composicao.Should().BeEmpty();
    }

    [Fact]
    public void Atualizar_ComDadosInvalidos_NaoDeveAlterarOEstado()
    {
        var produto = CriarValido(nome: "Bolo de chocolate");

        var resultado = produto.Atualizar("", TipoProducao.Porcoes, 5, "fatia", 10, Array.Empty<(Guid, decimal)>());

        resultado.IsFailure.Should().BeTrue();
        produto.Nome.Should().Be("Bolo de chocolate");
        produto.Composicao.Should().HaveCount(3);
    }

    [Fact]
    public void Atualizar_DevePreservarCriadoEmEAvancarAtualizadoEm()
    {
        var produto = CriarValido();
        var criadoOriginal = produto.CriadoEm;

        produto.Atualizar("Bolo", TipoProducao.Porcoes, 10, "fatia", 60, Array.Empty<(Guid, decimal)>());

        produto.CriadoEm.Should().Be(criadoOriginal);
        produto.AtualizadoEm.Should().BeOnOrAfter(criadoOriginal);
    }
}
