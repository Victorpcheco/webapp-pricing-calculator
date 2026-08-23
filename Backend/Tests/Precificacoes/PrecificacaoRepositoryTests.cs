using Domain.Entities.Precificacoes;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tests.Precificacoes;

/// <summary>
/// Persistência real (SQLite in-memory), não mock. Cobre o que o Moq não alcança:
/// o isolamento por usuário e a ordenação do histórico de simulações.
/// </summary>
public class PrecificacaoRepositoryTests : IDisposable
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid OutroUsuarioId = Guid.NewGuid();

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public PrecificacaoRepositoryTests()
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

    private static SimulacaoPreco NovaSimulacao(Guid? usuarioId = null, Guid? produtoId = null)
        => SimulacaoPreco.Criar(
            usuarioId ?? UsuarioId, produtoId ?? Guid.NewGuid(), "Bolo de chocolate", 2.782m, 40m, 3.9m, 30).Value;

    private async Task SemearAsync(params SimulacaoPreco[] simulacoes)
    {
        await using var context = NovoContexto();
        var repositorio = new PrecificacaoRepository(context);

        foreach (var simulacao in simulacoes)
            await repositorio.AdicionarAsync(simulacao);
    }

    /* ===================== ESCRITA ===================== */

    [Fact]
    public async Task AdicionarAsync_DevePersistirASimulacaoComOsValoresCalculados()
    {
        var simulacao = NovaSimulacao();

        await SemearAsync(simulacao);

        await using var context = NovoContexto();
        var salva = await context.SimulacoesPreco.SingleAsync();

        salva.ProdutoNome.Should().Be("Bolo de chocolate");
        salva.PrecoSugerido.Should().Be(3.8948m);
        salva.LucroTotalEstimado.Should().Be(33.54m);
    }

    [Fact]
    public async Task AtualizarAsync_DeveGravarOsNovosValoresRecalculados()
    {
        var simulacao = NovaSimulacao();
        await SemearAsync(simulacao);

        await using (var context = NovoContexto())
        {
            var repositorio = new PrecificacaoRepository(context);
            var alvo = await repositorio.ObterPorIdAsync(simulacao.Id, UsuarioId);

            alvo!.Atualizar(alvo.ProdutoId, "Brigadeiro tradicional", 0.86m, 100m, 2.0m, 100);

            await repositorio.AtualizarAsync(alvo);
        }

        await using var verificacao = NovoContexto();
        var salva = await verificacao.SimulacoesPreco.SingleAsync();

        salva.ProdutoNome.Should().Be("Brigadeiro tradicional");
        salva.LucroTotalEstimado.Should().Be(114m);
    }

    [Fact]
    public async Task RemoverAsync_DeveApagarApenasASimulacaoInformada()
    {
        var primeira = NovaSimulacao();
        var segunda = NovaSimulacao();
        await SemearAsync(primeira, segunda);

        await using (var context = NovoContexto())
        {
            var repositorio = new PrecificacaoRepository(context);
            var alvo = await repositorio.ObterPorIdAsync(primeira.Id, UsuarioId);
            await repositorio.RemoverAsync(alvo!);
        }

        await using var verificacao = NovoContexto();
        (await verificacao.SimulacoesPreco.SingleAsync()).Id.Should().Be(segunda.Id);
    }

    [Fact]
    public async Task RemoverTodosPorUsuarioAsync_NaoDeveTocarNasSimulacoesDeOutroUsuario()
    {
        await SemearAsync(NovaSimulacao(), NovaSimulacao(), NovaSimulacao(OutroUsuarioId));

        await using (var context = NovoContexto())
            await new PrecificacaoRepository(context).RemoverTodosPorUsuarioAsync(UsuarioId);

        await using var verificacao = NovoContexto();
        var restantes = await verificacao.SimulacoesPreco.ToListAsync();

        restantes.Should().ContainSingle().Which.UsuarioId.Should().Be(OutroUsuarioId);
    }

    /* ===================== LEITURA ===================== */

    [Fact]
    public async Task ObterPorIdAsync_DeSimulacaoDeOutroUsuario_DeveDevolverNull()
    {
        var simulacao = NovaSimulacao(OutroUsuarioId);
        await SemearAsync(simulacao);

        await using var context = NovoContexto();
        var encontrada = await new PrecificacaoRepository(context).ObterPorIdAsync(simulacao.Id, UsuarioId);

        encontrada.Should().BeNull();
    }

    [Fact]
    public async Task ListarPorUsuarioAsync_DeveTrazerAsMaisRecentesPrimeiro()
    {
        var primeira = NovaSimulacao();
        await SemearAsync(primeira);

        // CriadoEm é gravado na criação da entidade: um pequeno atraso garante a ordem
        await Task.Delay(10);
        var segunda = NovaSimulacao();
        await SemearAsync(segunda);

        await using var context = NovoContexto();
        var lista = await new PrecificacaoRepository(context).ListarPorUsuarioAsync(UsuarioId);

        lista.Select(s => s.Id).Should().ContainInOrder(segunda.Id, primeira.Id);
    }

    [Fact]
    public async Task ListarPorUsuarioAsync_NaoDeveVazarSimulacoesDeOutroUsuario()
    {
        await SemearAsync(NovaSimulacao(), NovaSimulacao(OutroUsuarioId));

        await using var context = NovoContexto();
        var lista = await new PrecificacaoRepository(context).ListarPorUsuarioAsync(UsuarioId);

        lista.Should().ContainSingle().Which.UsuarioId.Should().Be(UsuarioId);
    }
}
