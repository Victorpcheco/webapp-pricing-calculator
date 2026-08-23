// Application/Precificacoes/Services/PrecificacaoService.cs
using Application.Common;
using Application.Precificacoes.Commands;
using Application.Precificacoes.Queries;
using Application.Repositories;
using Domain.Common;
using Domain.Entities.Precificacoes;
using Domain.Entities.Produtos;

namespace Application.Precificacoes.Services;

public class PrecificacaoService : IScopedService
{
    private readonly IPrecificacaoRepository _precificacaoRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly ICustoRepository _custoRepository;
    private readonly ICurrentUserService _currentUser;

    public PrecificacaoService(
        IPrecificacaoRepository precificacaoRepository,
        IProdutoRepository produtoRepository,
        IInsumoRepository insumoRepository,
        ICustoRepository custoRepository,
        ICurrentUserService currentUser)
    {
        _precificacaoRepository = precificacaoRepository;
        _produtoRepository = produtoRepository;
        _insumoRepository = insumoRepository;
        _custoRepository = custoRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SimulacaoResult>> ListarAsync(ListarSimulacoesQuery query, CancellationToken ct = default)
    {
        var simulacoes = await _precificacaoRepository.ListarPorUsuarioAsync(_currentUser.UsuarioId, ct);
        return simulacoes.Select(ToResult).ToList();
    }

    public async Task<Result<SimulacaoResult>> CriarAsync(CriarSimulacaoCommand command, CancellationToken ct = default)
    {
        var produto = await ObterCustoAtualAsync(command.RecipeId, ct);
        if (produto.IsFailure)
            return Result<SimulacaoResult>.Failure(produto.Error);

        var resultado = SimulacaoPreco.Criar(
            usuarioId: _currentUser.UsuarioId,
            produtoId: command.RecipeId,
            produtoNome: produto.Value.Nome,
            custoBase: produto.Value.CustoUnitario,
            margem: command.Margin,
            precoPraticado: command.SalePrice,
            quantidade: command.Quantity
        );

        if (resultado.IsFailure)
            return Result<SimulacaoResult>.Failure(resultado.Error);

        await _precificacaoRepository.AdicionarAsync(resultado.Value, ct);
        return Result<SimulacaoResult>.Success(ToResult(resultado.Value));
    }

    public async Task<Result<SimulacaoResult>> AtualizarAsync(AtualizarSimulacaoCommand command, CancellationToken ct = default)
    {
        var simulacao = await _precificacaoRepository.ObterPorIdAsync(command.Id, _currentUser.UsuarioId, ct);
        if (simulacao is null)
            return Result<SimulacaoResult>.Failure("Simulação não encontrada.");

        var produto = await ObterCustoAtualAsync(command.RecipeId, ct);
        if (produto.IsFailure)
            return Result<SimulacaoResult>.Failure(produto.Error);

        var resultado = simulacao.Atualizar(
            produtoId: command.RecipeId,
            produtoNome: produto.Value.Nome,
            custoBase: produto.Value.CustoUnitario,
            margem: command.Margin,
            precoPraticado: command.SalePrice,
            quantidade: command.Quantity
        );

        if (resultado.IsFailure)
            return Result<SimulacaoResult>.Failure(resultado.Error);

        await _precificacaoRepository.AtualizarAsync(simulacao, ct);
        return Result<SimulacaoResult>.Success(ToResult(simulacao));
    }

    public async Task<Result> ExcluirAsync(ExcluirSimulacaoCommand command, CancellationToken ct = default)
    {
        var simulacao = await _precificacaoRepository.ObterPorIdAsync(command.Id, _currentUser.UsuarioId, ct);
        if (simulacao is null)
            return Result.Failure("Simulação não encontrada.");

        await _precificacaoRepository.RemoverAsync(simulacao, ct);
        return Result.Success();
    }

    public async Task<Result> LimparAsync(LimparSimulacoesCommand command, CancellationToken ct = default)
    {
        await _precificacaoRepository.RemoverTodosPorUsuarioAsync(_currentUser.UsuarioId, ct);
        return Result.Success();
    }

    /// <summary>
    /// Resolve o custo unitário vigente do produto — a mesma ficha calculada que
    /// o GET /api/produtos devolve. Consultado uma vez ao salvar; a simulação
    /// depois guarda esse valor como histórico, sem recalcular nas leituras seguintes.
    /// </summary>
    private async Task<Result<(string Nome, decimal CustoUnitario)>> ObterCustoAtualAsync(Guid produtoId, CancellationToken ct)
    {
        var produto = await _produtoRepository.ObterPorIdAsync(produtoId, _currentUser.UsuarioId, ct);
        if (produto is null)
            return Result<(string, decimal)>.Failure("Produto não encontrado.");

        var valorHora = await _custoRepository.ObterValorHoraAtualAsync(_currentUser.UsuarioId, ct);

        var insumoIds = produto.Composicao.Select(item => item.InsumoId).Distinct().ToList();
        var insumos = insumoIds.Count == 0
            ? new List<Domain.Entities.Insumos.Insumo>()
            : (await _insumoRepository.ListarPorIdsAsync(_currentUser.UsuarioId, insumoIds, ct)).ToList();

        var custoPorInsumo = insumos.ToDictionary(insumo => insumo.Id, insumo => insumo.PrecoUnitario);
        var ficha = produto.Calcular(custoPorInsumo, valorHora);

        return Result<(string, decimal)>.Success((produto.Nome, ficha.CustoUnitario));
    }

    private static SimulacaoResult ToResult(SimulacaoPreco s) => new(
        Id: s.Id,
        RecipeId: s.ProdutoId,
        RecipeName: s.ProdutoNome,
        Cost: s.CustoBase,
        Margin: s.Margem,
        Suggested: s.PrecoSugerido,
        SalePrice: s.PrecoPraticado,
        Quantity: s.Quantidade,
        Profit: s.LucroUnitario,
        RealMargin: s.MargemReal,
        Revenue: s.ReceitaEstimada,
        TotalProfit: s.LucroTotalEstimado,
        CreatedAt: s.CriadoEm
    );
}
