using Domain.Entities.Custos;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tests.Custos;

/// <summary>
/// Persistência real (SQLite in-memory). Cobre principalmente o
/// ObterValorHoraAtualAsync, cuja ordenação alimenta o custo de trabalho
/// das fichas técnicas em Produtos.
/// </summary>
public class CustoRepositoryTests : IDisposable
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public CustoRepositoryTests()
    {
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

    private static CustoOperacional NovoCusto(
        Guid? usuarioId = null,
        string descricao = "Configuração atual",
        decimal proLabore = 3000m,
        int horas = 176)
        => CustoOperacional.Criar(
            usuarioId ?? UsuarioId, descricao, proLabore, horas,
            450m, 30m, 180m, 70m, true, 80.90m, 5m).Value;

    /// <summary>
    /// CriadoEm é encapsulado e vem de DateTime.UtcNow — chamadas seguidas podem
    /// cair no mesmo tick. Fixar a data deixa os testes de ordenação determinísticos.
    /// </summary>
    private static CustoOperacional ComCriadoEm(CustoOperacional custo, DateTime data)
    {
        typeof(CustoOperacional)
            .GetProperty(nameof(CustoOperacional.CriadoEm))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(custo, new object[] { data });
        return custo;
    }

    /* ===================== VALOR DA HORA ATUAL ===================== */

    [Fact]
    public async Task ObterValorHoraAtualAsync_DeveDevolverODaConfiguracaoMaisRecente()
    {
        var antigo = ComCriadoEm(NovoCusto(proLabore: 2000m), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var recente = ComCriadoEm(NovoCusto(proLabore: 5000m), new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        await using (var context = NovoContexto())
        {
            // Inseridos ao contrário de propósito: se a data fixada não valesse,
            // a ordem de inserção devolveria o antigo e o teste passaria à toa
            var repositorio = new CustoRepository(context);
            await repositorio.AdicionarAsync(recente);
            await repositorio.AdicionarAsync(antigo);
        }

        await using var leitura = NovoContexto();
        var valorHora = await new CustoRepository(leitura).ObterValorHoraAtualAsync(UsuarioId);

        valorHora.Should().Be(recente.ValorHora);
        valorHora.Should().NotBe(antigo.ValorHora);
    }

    [Fact]
    public async Task ObterValorHoraAtualAsync_SemNenhumaConfiguracao_DeveDevolverZero()
    {
        // É o que faz a tela de Produtos mostrar "Aguardando configuração"
        await using var leitura = NovoContexto();

        var valorHora = await new CustoRepository(leitura).ObterValorHoraAtualAsync(UsuarioId);

        valorHora.Should().Be(0m);
    }

    [Fact]
    public async Task ObterValorHoraAtualAsync_NaoDeveEnxergarConfiguracaoDeOutroUsuario()
    {
        var outroUsuario = Guid.NewGuid();

        await using (var context = NovoContexto())
            await new CustoRepository(context).AdicionarAsync(NovoCusto(usuarioId: outroUsuario));

        await using var leitura = NovoContexto();
        var valorHora = await new CustoRepository(leitura).ObterValorHoraAtualAsync(UsuarioId);

        valorHora.Should().Be(0m);
    }

    /* ===================== ESCRITA E LEITURA ===================== */

    [Fact]
    public async Task AdicionarAsync_DevePersistirOsCamposCalculados()
    {
        var custo = NovoCusto();

        await using (var context = NovoContexto())
            await new CustoRepository(context).AdicionarAsync(custo);

        await using var leitura = NovoContexto();
        var salvo = await new CustoRepository(leitura).ObterPorIdAsync(custo.Id, UsuarioId);

        salvo.Should().NotBeNull();
        salvo!.EnergiaReal.Should().Be(135m);
        salvo.GasReal.Should().Be(126m);
        salvo.CustoMensal.Should().Be(3508.9950m);
        salvo.ValorHora.Should().BeApproximately(19.9375m, 0.0001m);
    }

    [Fact]
    public async Task AtualizarAsync_DeveGravarOsValoresRecalculados()
    {
        var custo = NovoCusto();

        await using (var context = NovoContexto())
            await new CustoRepository(context).AdicionarAsync(custo);

        await using (var context = NovoContexto())
        {
            var repositorio = new CustoRepository(context);
            var carregado = await repositorio.ObterPorIdAsync(custo.Id, UsuarioId);
            carregado!.Atualizar(4000m, 200, 500m, 50m, 200m, 50m, false, 0m, 0m);
            await repositorio.AtualizarAsync(carregado);
        }

        await using var leitura = NovoContexto();
        var atualizado = await new CustoRepository(leitura).ObterPorIdAsync(custo.Id, UsuarioId);

        atualizado!.ProLabore.Should().Be(4000m);
        atualizado.CustoMensal.Should().Be(4350m);
        atualizado.ValorHora.Should().Be(21.75m);
    }

    [Fact]
    public async Task ListarPorUsuarioAsync_DeveOrdenarDoMaisRecenteParaOMaisAntigo()
    {
        await using (var context = NovoContexto())
        {
            var repositorio = new CustoRepository(context);
            await repositorio.AdicionarAsync(
                ComCriadoEm(NovoCusto(descricao: "Janeiro"), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            await repositorio.AdicionarAsync(
                ComCriadoEm(NovoCusto(descricao: "Junho"), new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)));
            await repositorio.AdicionarAsync(
                ComCriadoEm(NovoCusto(descricao: "Março"), new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
        }

        await using var leitura = NovoContexto();
        var historico = await new CustoRepository(leitura).ListarPorUsuarioAsync(UsuarioId);

        historico.Select(c => c.Descricao).Should().ContainInOrder("Junho", "Março", "Janeiro");
    }

    [Fact]
    public async Task ObterPorIdAsync_ComCustoDeOutroUsuario_NaoDeveEncontrar()
    {
        var custo = NovoCusto();

        await using (var context = NovoContexto())
            await new CustoRepository(context).AdicionarAsync(custo);

        await using var leitura = NovoContexto();
        var alheio = await new CustoRepository(leitura).ObterPorIdAsync(custo.Id, Guid.NewGuid());

        alheio.Should().BeNull();
    }

    /* ===================== EXCLUSÃO ===================== */

    [Fact]
    public async Task RemoverAsync_DeveApagarApenasOItemInformado()
    {
        var manter = NovoCusto(descricao: "Manter");
        var remover = NovoCusto(descricao: "Remover");

        await using (var context = NovoContexto())
        {
            var repositorio = new CustoRepository(context);
            await repositorio.AdicionarAsync(manter);
            await repositorio.AdicionarAsync(remover);
        }

        await using (var context = NovoContexto())
        {
            var repositorio = new CustoRepository(context);
            var carregado = await repositorio.ObterPorIdAsync(remover.Id, UsuarioId);
            await repositorio.RemoverAsync(carregado!);
        }

        await using var leitura = NovoContexto();
        var restantes = await new CustoRepository(leitura).ListarPorUsuarioAsync(UsuarioId);

        restantes.Should().ContainSingle();
        restantes.Single().Descricao.Should().Be("Manter");
    }

    [Fact]
    public async Task RemoverTodosPorUsuarioAsync_NaoDeveTocarNoHistoricoDeOutroUsuario()
    {
        var outroUsuario = Guid.NewGuid();

        await using (var context = NovoContexto())
        {
            var repositorio = new CustoRepository(context);
            await repositorio.AdicionarAsync(NovoCusto());
            await repositorio.AdicionarAsync(NovoCusto());
            await repositorio.AdicionarAsync(NovoCusto(usuarioId: outroUsuario));
        }

        await using (var context = NovoContexto())
            await new CustoRepository(context).RemoverTodosPorUsuarioAsync(UsuarioId);

        await using var leitura = NovoContexto();
        var repositorioLeitura = new CustoRepository(leitura);

        (await repositorioLeitura.ListarPorUsuarioAsync(UsuarioId)).Should().BeEmpty();
        (await repositorioLeitura.ListarPorUsuarioAsync(outroUsuario)).Should().ContainSingle();
    }

    [Fact]
    public async Task RemoverTodosPorUsuarioAsync_SemHistorico_NaoDeveFalhar()
    {
        await using var context = NovoContexto();

        var act = async () => await new CustoRepository(context).RemoverTodosPorUsuarioAsync(UsuarioId);

        await act.Should().NotThrowAsync();
    }
}
