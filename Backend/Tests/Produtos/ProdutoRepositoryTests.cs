using Domain.Entities.Produtos;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tests.Produtos;

/// <summary>
/// Persistência real (SQLite in-memory), não mock. Cobre o que o Moq não alcança:
/// como o EF sincroniza a composição owned no UPDATE.
///
/// Regressão do DbUpdateConcurrencyException — "expected to affect 1 row(s), but
/// actually affected 0": chamar Update() num produto rastreado marcava as linhas
/// de composição recém-criadas como Modified, e o EF tentava atualizar linhas
/// que ainda não existiam no banco.
/// </summary>
public class ProdutoRepositoryTests : IDisposable
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid Farinha = Guid.NewGuid();
    private static readonly Guid Leite = Guid.NewGuid();
    private static readonly Guid Caixa = Guid.NewGuid();

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public ProdutoRepositoryTests()
    {
        // Conexão aberta mantém o banco em memória vivo entre os contextos
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private AppDbContext NovoContexto() => new(_options);

    private static Produto NovoProduto(IEnumerable<(Guid, decimal)>? composicao = null)
        => Produto.Criar(
            UsuarioId, "Bolo de chocolate", TipoProducao.Porcoes, 10, "fatia", 60,
            composicao ?? new[] { (Farinha, 500m), (Leite, 250m) }).Value;

    /* ===================== ESCRITA ===================== */

    [Fact]
    public async Task AdicionarAsync_DevePersistirOProdutoComSuaComposicao()
    {
        var produto = NovoProduto();

        await using (var context = NovoContexto())
            await new ProdutoRepository(context).AdicionarAsync(produto);

        await using var leitura = NovoContexto();
        var salvo = await new ProdutoRepository(leitura).ObterPorIdAsync(produto.Id, UsuarioId);

        salvo.Should().NotBeNull();
        salvo!.Nome.Should().Be("Bolo de chocolate");
        salvo.Composicao.Should().HaveCount(2);
    }

    [Fact]
    public async Task AtualizarAsync_ComComposicaoDiferente_DeveSubstituirAsLinhas()
    {
        // Este é o caminho que quebrava: linhas novas + linhas removidas no mesmo save
        var produto = NovoProduto();

        await using (var context = NovoContexto())
            await new ProdutoRepository(context).AdicionarAsync(produto);

        await using (var context = NovoContexto())
        {
            var repositorio = new ProdutoRepository(context);
            var carregado = await repositorio.ObterPorIdAsync(produto.Id, UsuarioId);

            var resultado = carregado!.Atualizar(
                "Bolo simples", TipoProducao.ProdutoInteiro, 1, "bolo", 30,
                new[] { (Caixa, 3m) });
            resultado.IsSuccess.Should().BeTrue(resultado.Error);

            await repositorio.AtualizarAsync(carregado);
        }

        await using var leitura = NovoContexto();
        var atualizado = await new ProdutoRepository(leitura).ObterPorIdAsync(produto.Id, UsuarioId);

        atualizado!.Nome.Should().Be("Bolo simples");
        atualizado.TipoProducao.Should().Be(TipoProducao.ProdutoInteiro);
        atualizado.Rendimento.Should().Be(1);
        atualizado.Composicao.Should().HaveCount(1);
        atualizado.Composicao.Single().InsumoId.Should().Be(Caixa);
        atualizado.Composicao.Single().Quantidade.Should().Be(3m);
    }

    [Fact]
    public async Task AtualizarAsync_ComComposicaoVazia_DeveApagarTodasAsLinhas()
    {
        var produto = NovoProduto();

        await using (var context = NovoContexto())
            await new ProdutoRepository(context).AdicionarAsync(produto);

        await using (var context = NovoContexto())
        {
            var repositorio = new ProdutoRepository(context);
            var carregado = await repositorio.ObterPorIdAsync(produto.Id, UsuarioId);
            carregado!.Atualizar("Bolo", TipoProducao.Porcoes, 10, "fatia", 60, Array.Empty<(Guid, decimal)>());
            await repositorio.AtualizarAsync(carregado);
        }

        await using var leitura = NovoContexto();
        var atualizado = await new ProdutoRepository(leitura).ObterPorIdAsync(produto.Id, UsuarioId);

        atualizado!.Composicao.Should().BeEmpty();
    }

    [Fact]
    public async Task AtualizarAsync_SemMexerNaComposicao_DeveSalvarApenasOsEscalares()
    {
        var produto = NovoProduto();

        await using (var context = NovoContexto())
            await new ProdutoRepository(context).AdicionarAsync(produto);

        await using (var context = NovoContexto())
        {
            var repositorio = new ProdutoRepository(context);
            var carregado = await repositorio.ObterPorIdAsync(produto.Id, UsuarioId);
            carregado!.Atualizar("Outro nome", TipoProducao.Porcoes, 20, "pedaço", 90,
                new[] { (Farinha, 500m), (Leite, 250m) });
            await repositorio.AtualizarAsync(carregado);
        }

        await using var leitura = NovoContexto();
        var atualizado = await new ProdutoRepository(leitura).ObterPorIdAsync(produto.Id, UsuarioId);

        atualizado!.Nome.Should().Be("Outro nome");
        atualizado.Rendimento.Should().Be(20);
        atualizado.Composicao.Should().HaveCount(2);
    }

    /* ===================== EXCLUSÃO ===================== */

    [Fact]
    public async Task RemoverAsync_DeveApagarOProdutoEAComposicaoEmCascata()
    {
        var produto = NovoProduto();

        await using (var context = NovoContexto())
            await new ProdutoRepository(context).AdicionarAsync(produto);

        await using (var context = NovoContexto())
        {
            var repositorio = new ProdutoRepository(context);
            var carregado = await repositorio.ObterPorIdAsync(produto.Id, UsuarioId);
            await repositorio.RemoverAsync(carregado!);
        }

        await using var leitura = NovoContexto();
        (await new ProdutoRepository(leitura).ObterPorIdAsync(produto.Id, UsuarioId)).Should().BeNull();
        (await leitura.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM produto_composicao")
            .SingleAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RemoverTodosPorUsuarioAsync_DeveLevarAsComposicoesJunto()
    {
        await using (var context = NovoContexto())
        {
            var repositorio = new ProdutoRepository(context);
            await repositorio.AdicionarAsync(NovoProduto());
            await repositorio.AdicionarAsync(NovoProduto(new[] { (Caixa, 1m) }));
        }

        await using (var context = NovoContexto())
            await new ProdutoRepository(context).RemoverTodosPorUsuarioAsync(UsuarioId);

        await using var leitura = NovoContexto();
        (await new ProdutoRepository(leitura).ListarPorUsuarioAsync(UsuarioId, null)).Should().BeEmpty();
        (await leitura.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM produto_composicao")
            .SingleAsync()).Should().Be(0);
    }

    /* ===================== ISOLAMENTO POR USUÁRIO ===================== */

    [Fact]
    public async Task ObterPorIdAsync_ComProdutoDeOutroUsuario_NaoDeveEncontrar()
    {
        var produto = NovoProduto();

        await using (var context = NovoContexto())
            await new ProdutoRepository(context).AdicionarAsync(produto);

        await using var leitura = NovoContexto();
        var alheio = await new ProdutoRepository(leitura).ObterPorIdAsync(produto.Id, Guid.NewGuid());

        alheio.Should().BeNull();
    }

    [Fact]
    public async Task RemoverTodosPorUsuarioAsync_NaoDeveTocarNosProdutosDeOutroUsuario()
    {
        var outroUsuario = Guid.NewGuid();
        var doOutro = Produto.Criar(outroUsuario, "Torta", TipoProducao.Porcoes, 8, "fatia", 40,
            new[] { (Farinha, 300m) }).Value;

        await using (var context = NovoContexto())
        {
            var repositorio = new ProdutoRepository(context);
            await repositorio.AdicionarAsync(NovoProduto());
            await repositorio.AdicionarAsync(doOutro);
        }

        await using (var context = NovoContexto())
            await new ProdutoRepository(context).RemoverTodosPorUsuarioAsync(UsuarioId);

        await using var leitura = NovoContexto();
        var restantes = await new ProdutoRepository(leitura).ListarPorUsuarioAsync(outroUsuario, null);

        restantes.Should().HaveCount(1);
        restantes.Single().Composicao.Should().HaveCount(1);
    }

    /* ===================== LEITURA ===================== */

    [Fact]
    public async Task ListarPorUsuarioAsync_DeveTrazerAComposicaoJunto()
    {
        await using (var context = NovoContexto())
            await new ProdutoRepository(context).AdicionarAsync(NovoProduto());

        await using var leitura = NovoContexto();
        var produtos = await new ProdutoRepository(leitura).ListarPorUsuarioAsync(UsuarioId, null);

        produtos.Should().HaveCount(1);
        produtos.Single().Composicao.Should().HaveCount(2);
    }
}
