// Application/Resultados/Services/ResultadoService.cs
using Application.Common;
using Application.Repositories;
using Application.Resultados.Queries;
using Domain.Common;
using Domain.Entities.Insumos;
using Domain.Entities.Precificacoes;
using Domain.Entities.Produtos;

namespace Application.Resultados.Services;

/// <summary>
/// Consolida fichas técnicas (Produtos) e simulações de preço (Precificação) na
/// visão de desempenho da tela "Meus Resultados". Não persiste nada: é uma
/// leitura pura sobre os dois agregados já existentes.
/// </summary>
public class ResultadoService : IScopedService
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly ICustoRepository _custoRepository;
    private readonly IPrecificacaoRepository _precificacaoRepository;
    private readonly ICurrentUserService _currentUser;

    public ResultadoService(
        IProdutoRepository produtoRepository,
        IInsumoRepository insumoRepository,
        ICustoRepository custoRepository,
        IPrecificacaoRepository precificacaoRepository,
        ICurrentUserService currentUser)
    {
        _produtoRepository = produtoRepository;
        _insumoRepository = insumoRepository;
        _custoRepository = custoRepository;
        _precificacaoRepository = precificacaoRepository;
        _currentUser = currentUser;
    }

    public async Task<ResultadoListResult> ListarAsync(ListarResultadosQuery query, CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;
        var periodo = query.Periodo?.Trim().ToLowerInvariant() ?? "all";

        var produtos = await _produtoRepository.ListarPorUsuarioAsync(_currentUser.UsuarioId, null, ct);
        var custoAtualPorProduto = await ResolverCustoAtualAsync(produtos, ct);

        var simulacoes = await _precificacaoRepository.ListarPorUsuarioAsync(_currentUser.UsuarioId, ct);
        var simulacoesNoPeriodo = simulacoes
            .Where(s => NoPeriodo(s.CriadoEm, periodo, query.Inicio, query.Fim, agora))
            .ToList();

        // Com simulações no período, a tabela mostra cada tentativa de preço;
        // sem nenhuma, cai para a lista de receitas só com o custo (como no mockup).
        var linhas = simulacoesNoPeriodo.Count > 0
            ? simulacoesNoPeriodo.Select(s => MontarLinhaPrecificada(s, custoAtualPorProduto)).ToList()
            : produtos
                .Where(p => NoPeriodo(p.AtualizadoEm, periodo, query.Inicio, query.Fim, agora))
                .Select(p => MontarLinhaSemPreco(p, custoAtualPorProduto))
                .ToList();

        var totais = CalcularResumo(simulacoesNoPeriodo, custoAtualPorProduto, linhas.Count);

        return new ResultadoListResult(linhas, totais);
    }

    /// <summary>
    /// Custo unitário vigente de cada produto — a mesma ficha calculada que o
    /// GET /api/produtos devolve. Resolvido em lote para não repetir a consulta
    /// de insumos uma vez por simulação.
    /// </summary>
    private async Task<IReadOnlyDictionary<Guid, decimal>> ResolverCustoAtualAsync(
        IReadOnlyList<Produto> produtos,
        CancellationToken ct)
    {
        if (produtos.Count == 0)
            return new Dictionary<Guid, decimal>();

        var valorHora = await _custoRepository.ObterValorHoraAtualAsync(_currentUser.UsuarioId, ct);

        var insumoIds = produtos.SelectMany(p => p.Composicao).Select(i => i.InsumoId).Distinct().ToList();
        var insumos = insumoIds.Count == 0
            ? new List<Insumo>()
            : (await _insumoRepository.ListarPorIdsAsync(_currentUser.UsuarioId, insumoIds, ct)).ToList();

        var custoPorInsumo = insumos.ToDictionary(i => i.Id, i => i.PrecoUnitario);

        return produtos.ToDictionary(p => p.Id, p => p.Calcular(custoPorInsumo, valorHora).CustoUnitario);
    }

    /// <summary>
    /// Custo usado no desempenho: o valor atual do produto quando ele ainda existe;
    /// caso tenha sido excluído, cai para o retrato gravado na própria simulação.
    /// </summary>
    private static decimal ResolverCusto(SimulacaoPreco simulacao, IReadOnlyDictionary<Guid, decimal> custoAtualPorProduto)
        => custoAtualPorProduto.TryGetValue(simulacao.ProdutoId, out var custo) ? custo : simulacao.CustoBase;

    private static ResultadoRow MontarLinhaPrecificada(
        SimulacaoPreco simulacao,
        IReadOnlyDictionary<Guid, decimal> custoAtualPorProduto)
    {
        // Lucro e margem são recalculados com o custo vigente — diferente da simulação salva,
        // que é um retrato congelado do dia em que o preço foi testado.
        var custo = ResolverCusto(simulacao, custoAtualPorProduto);
        var lucro = simulacao.PrecoPraticado - custo;
        var margem = simulacao.PrecoPraticado > 0 ? (lucro / simulacao.PrecoPraticado) * 100m : 0m;

        return new ResultadoRow(
            ProductId: simulacao.ProdutoId,
            Name: simulacao.ProdutoNome,
            Unit: "unidade",
            Cost: custo,
            SalePrice: simulacao.PrecoPraticado,
            Profit: lucro,
            Margin: margem,
            Priced: true
        );
    }

    private static ResultadoRow MontarLinhaSemPreco(Produto produto, IReadOnlyDictionary<Guid, decimal> custoAtualPorProduto)
        => new(
            ProductId: produto.Id,
            Name: produto.Nome,
            Unit: produto.NomeUnidade,
            Cost: custoAtualPorProduto.TryGetValue(produto.Id, out var custo) ? custo : 0m,
            SalePrice: null,
            Profit: null,
            Margin: null,
            Priced: false
        );

    /// <summary>
    /// Receita e lucro somados ponderados pela quantidade estimada de cada simulação —
    /// diferente das linhas da tabela, que mostram o valor por unidade.
    /// Sem simulações no período, os KPIs financeiros ficam zerados mesmo que
    /// existam receitas sem preço (a tabela cai para o modo "sem preço").
    /// </summary>
    private static ResultadoResumo CalcularResumo(
        IReadOnlyList<SimulacaoPreco> simulacoesNoPeriodo,
        IReadOnlyDictionary<Guid, decimal> custoAtualPorProduto,
        int analysedCount)
    {
        if (simulacoesNoPeriodo.Count == 0)
            return ResultadoResumo.Vazio with { AnalysedCount = analysedCount };

        decimal receita = 0m, lucro = 0m;
        foreach (var simulacao in simulacoesNoPeriodo)
        {
            var custo = ResolverCusto(simulacao, custoAtualPorProduto);
            receita += simulacao.PrecoPraticado * simulacao.Quantidade;
            lucro += (simulacao.PrecoPraticado - custo) * simulacao.Quantidade;
        }

        var margem = receita > 0 ? (lucro / receita) * 100m : 0m;
        return new ResultadoResumo(lucro, receita, margem, analysedCount);
    }

    /// <summary>Mesma janela de datas usada pela tela: hoje, últimos 7 dias, mês corrente ou intervalo customizado.</summary>
    private static bool NoPeriodo(DateTime data, string periodo, DateTime? inicio, DateTime? fim, DateTime agora)
    {
        switch (periodo)
        {
            case "today":
                return data.Date == agora.Date;
            case "week":
                return data >= agora.AddDays(-7);
            case "month":
                return data.Month == agora.Month && data.Year == agora.Year;
            case "custom":
                if (inicio.HasValue && data < inicio.Value.Date) return false;
                if (fim.HasValue && data >= fim.Value.Date.AddDays(1)) return false;
                return true;
            default:
                return true; // "all"
        }
    }
}
