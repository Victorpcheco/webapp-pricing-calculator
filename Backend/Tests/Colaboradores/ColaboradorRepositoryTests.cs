using Application.Colaboradores.Services;
using Domain.Entities.Colaboradores;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tests.Colaboradores;

/// <summary>
/// Persistência real (SQLite in-memory), não mock. Cobre o que o Moq não alcança:
/// o isolamento por usuário e a agregação dos cards de estatística em uma consulta só.
///
/// O filtro de busca não entra aqui: ele usa ILike, função exclusiva do Npgsql,
/// que o provider SQLite não sabe traduzir.
/// </summary>
public class ColaboradorRepositoryTests : IDisposable
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid OutroUsuarioId = Guid.NewGuid();

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public ColaboradorRepositoryTests()
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

    private static Colaborador NovoClt(Guid? usuarioId = null, decimal salario = 1900m)
        => Colaborador.Criar(
            usuarioId ?? UsuarioId, "COL-01", "Juliana Ferreira", "Confeiteira",
            TipoContratacao.Clt, StatusColaborador.Ativo, null, salario, null, "(11) 98888-1234").Value;

    private static Colaborador NovoFreelancer(
        Guid? usuarioId = null,
        decimal valor = 45m,
        FrequenciaFreelancer frequencia = FrequenciaFreelancer.PorHora)
        => Colaborador.Criar(
            usuarioId ?? UsuarioId, "COL-02", "Rafael Souza", "Designer de embalagens",
            TipoContratacao.Freelancer, StatusColaborador.Ativo, null, valor, frequencia, null).Value;

    private async Task SemearAsync(params Colaborador[] colaboradores)
    {
        await using var context = NovoContexto();
        var repositorio = new ColaboradorRepository(context);

        foreach (var colaborador in colaboradores)
            await repositorio.AdicionarAsync(colaborador);
    }

    /* ===================== ESCRITA ===================== */

    [Fact]
    public async Task AdicionarAsync_DevePersistirOColaboradorComOCustoJaCalculado()
    {
        var colaborador = NovoClt();

        await SemearAsync(colaborador);

        await using var context = NovoContexto();
        var salvo = await context.Colaboradores.SingleAsync();

        salvo.Nome.Should().Be("Juliana Ferreira");
        salvo.Cargo.Should().Be("Confeiteira");
        salvo.TipoContratacao.Should().Be(TipoContratacao.Clt);
        salvo.FrequenciaPagamento.Should().BeNull();
        salvo.CustoMensal.Should().Be(2421.4444m);
    }

    [Fact]
    public async Task AdicionarAsync_DeFreelancer_DevePersistirAFormaDePagamento()
    {
        await SemearAsync(NovoFreelancer(frequencia: FrequenciaFreelancer.PorServico));

        await using var context = NovoContexto();
        var salvo = await context.Colaboradores.SingleAsync();

        salvo.FrequenciaPagamento.Should().Be(FrequenciaFreelancer.PorServico);
        salvo.CustoMensal.Should().Be(0m);
    }

    [Fact]
    public async Task AtualizarAsync_DeveGravarOCustoReprovisionado()
    {
        var colaborador = NovoClt();
        await SemearAsync(colaborador);

        await using (var context = NovoContexto())
        {
            var repositorio = new ColaboradorRepository(context);
            var alvo = await repositorio.ObterPorIdAsync(colaborador.Id, UsuarioId);

            alvo!.Atualizar(
                "COL-01", "Juliana Ferreira", "Confeiteira chefe", TipoContratacao.Clt,
                StatusColaborador.Inativo, null, 2400m, null, null);

            await repositorio.AtualizarAsync(alvo);
        }

        await using var verificacao = NovoContexto();
        var salvo = await verificacao.Colaboradores.SingleAsync();

        salvo.Cargo.Should().Be("Confeiteira chefe");
        salvo.Status.Should().Be(StatusColaborador.Inativo);
        salvo.CustoMensal.Should().Be(3058.6667m);
    }

    [Fact]
    public async Task RemoverAsync_DeveApagarApenasOColaboradorInformado()
    {
        var clt = NovoClt();
        var freelancer = NovoFreelancer();
        await SemearAsync(clt, freelancer);

        await using (var context = NovoContexto())
        {
            var repositorio = new ColaboradorRepository(context);
            var alvo = await repositorio.ObterPorIdAsync(clt.Id, UsuarioId);
            await repositorio.RemoverAsync(alvo!);
        }

        await using var verificacao = NovoContexto();
        (await verificacao.Colaboradores.SingleAsync()).Id.Should().Be(freelancer.Id);
    }

    [Fact]
    public async Task RemoverTodosPorUsuarioAsync_NaoDeveTocarNaEquipeDeOutroUsuario()
    {
        await SemearAsync(NovoClt(), NovoFreelancer(), NovoClt(OutroUsuarioId));

        await using (var context = NovoContexto())
            await new ColaboradorRepository(context).RemoverTodosPorUsuarioAsync(UsuarioId);

        await using var verificacao = NovoContexto();
        var restantes = await verificacao.Colaboradores.ToListAsync();

        restantes.Should().ContainSingle().Which.UsuarioId.Should().Be(OutroUsuarioId);
    }

    /* ===================== LEITURA ===================== */

    [Fact]
    public async Task ObterPorIdAsync_DeColaboradorDeOutroUsuario_DeveDevolverNull()
    {
        var colaborador = NovoClt(OutroUsuarioId);
        await SemearAsync(colaborador);

        await using var context = NovoContexto();
        var encontrado = await new ColaboradorRepository(context).ObterPorIdAsync(colaborador.Id, UsuarioId);

        encontrado.Should().BeNull();
    }

    [Fact]
    public async Task ListarPorUsuarioAsync_DeveTrazerOsMaisRecentesPrimeiro()
    {
        var primeiro = NovoClt();
        await SemearAsync(primeiro);

        // CriadoEm é gravado na criação da entidade: um pequeno atraso garante a ordem
        await Task.Delay(10);
        var segundo = NovoFreelancer();
        await SemearAsync(segundo);

        await using var context = NovoContexto();
        var lista = await new ColaboradorRepository(context).ListarPorUsuarioAsync(UsuarioId, null, null);

        lista.Select(c => c.Id).Should().ContainInOrder(segundo.Id, primeiro.Id);
    }

    [Fact]
    public async Task ListarPorUsuarioAsync_ComFiltroDeContratacao_DeveDevolverSoOTipoPedido()
    {
        await SemearAsync(NovoClt(), NovoFreelancer());

        await using var context = NovoContexto();
        var lista = await new ColaboradorRepository(context)
            .ListarPorUsuarioAsync(UsuarioId, null, TipoContratacao.Freelancer);

        lista.Should().ContainSingle().Which.TipoContratacao.Should().Be(TipoContratacao.Freelancer);
    }

    [Fact]
    public async Task ListarPorUsuarioAsync_NaoDeveVazarColaboradoresDeOutroUsuario()
    {
        await SemearAsync(NovoClt(), NovoClt(OutroUsuarioId));

        await using var context = NovoContexto();
        var lista = await new ColaboradorRepository(context).ListarPorUsuarioAsync(UsuarioId, null, null);

        lista.Should().ContainSingle().Which.UsuarioId.Should().Be(UsuarioId);
    }

    /* ===================== RESUMO ===================== */

    [Fact]
    public async Task ObterResumoAsync_DeveSomarOCustoDaEquipeEContarPorContratacao()
    {
        await SemearAsync(
            NovoClt(),                                                        // custo 2421,4444
            NovoFreelancer(frequencia: FrequenciaFreelancer.Mensal, valor: 2500m), // custo 2500
            NovoFreelancer());                                                // por hora: custo 0

        await using var context = NovoContexto();
        var resumo = await new ColaboradorRepository(context).ObterResumoAsync(UsuarioId);

        resumo.Total.Should().Be(3);
        resumo.CltCount.Should().Be(1);
        resumo.FreelancerCount.Should().Be(2);
        resumo.PayrollValue.Should().Be(4921.4444m);
    }

    [Fact]
    public async Task ObterResumoAsync_SemColaboradores_DeveDevolverOResumoVazio()
    {
        await using var context = NovoContexto();
        var resumo = await new ColaboradorRepository(context).ObterResumoAsync(UsuarioId);

        resumo.Should().Be(ColaboradoresResumo.Vazio);
    }

    [Fact]
    public async Task ObterResumoAsync_NaoDeveContabilizarAEquipeDeOutroUsuario()
    {
        await SemearAsync(NovoClt(), NovoClt(OutroUsuarioId, 5000m));

        await using var context = NovoContexto();
        var resumo = await new ColaboradorRepository(context).ObterResumoAsync(UsuarioId);

        resumo.Total.Should().Be(1);
        resumo.PayrollValue.Should().Be(2421.4444m);
    }
}
