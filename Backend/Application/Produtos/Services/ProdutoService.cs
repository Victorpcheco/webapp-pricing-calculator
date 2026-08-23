// Application/Produtos/Services/ProdutoService.cs
using Application.Common;
using Application.Produtos.Commands;
using Application.Produtos.Queries;
using Application.Repositories;
using Domain.Common;
using Domain.Entities.Insumos;
using Domain.Entities.Produtos;

namespace Application.Produtos.Services;

public class ProdutoService : IScopedService
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly ICustoRepository _custoRepository;
    private readonly ICurrentUserService _currentUser;

    public ProdutoService(
        IProdutoRepository produtoRepository,
        IInsumoRepository insumoRepository,
        ICustoRepository custoRepository,
        ICurrentUserService currentUser)
    {
        _produtoRepository = produtoRepository;
        _insumoRepository = insumoRepository;
        _custoRepository = custoRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<ProdutosListResult>> ListarAsync(ListarProdutosQuery query, CancellationToken ct = default)
    {
        var produtos = await _produtoRepository.ListarPorUsuarioAsync(_currentUser.UsuarioId, query.Nome, ct);
        var valorHora = await _custoRepository.ObterValorHoraAtualAsync(_currentUser.UsuarioId, ct);

        // Um único round-trip resolve os insumos de todas as fichas da página
        var insumos = await ResolverInsumosAsync(
            produtos.SelectMany(p => p.Composicao).Select(i => i.InsumoId), ct);

        var dados = produtos.Select(produto => Montar(produto, insumos, valorHora)).ToList();

        return Result<ProdutosListResult>.Success(
            new ProdutosListResult(dados, new ProdutosResumo(dados.Count, valorHora)));
    }

    public async Task<Result<ProdutoResult>> CriarAsync(CriarProdutoCommand command, CancellationToken ct = default)
    {
        if (!TipoProducaoExtensions.TryParse(command.ProductionType, out var tipoProducao))
            return Result<ProdutoResult>.Failure("O tipo de produção deve ser 'Produto inteiro' ou 'Porções'.");

        var composicao = MapearComposicao(command.Composition);

        var validacaoInsumos = await ValidarInsumosAsync(composicao, ct);
        if (validacaoInsumos.IsFailure)
            return Result<ProdutoResult>.Failure(validacaoInsumos.Error);

        var resultado = Produto.Criar(
            usuarioId: _currentUser.UsuarioId,
            nome: command.Name,
            tipoProducao: tipoProducao,
            rendimento: command.YieldAmount,
            nomeUnidade: command.YieldName,
            tempoProducaoMinutos: command.ProductionTime,
            composicao: composicao
        );

        if (resultado.IsFailure)
            return Result<ProdutoResult>.Failure(resultado.Error);

        await _produtoRepository.AdicionarAsync(resultado.Value, ct);

        return Result<ProdutoResult>.Success(await MontarAsync(resultado.Value, ct));
    }

    public async Task<Result<ProdutoResult>> AtualizarAsync(AtualizarProdutoCommand command, CancellationToken ct = default)
    {
        if (!TipoProducaoExtensions.TryParse(command.ProductionType, out var tipoProducao))
            return Result<ProdutoResult>.Failure("O tipo de produção deve ser 'Produto inteiro' ou 'Porções'.");

        var produto = await _produtoRepository.ObterPorIdAsync(command.Id, _currentUser.UsuarioId, ct);
        if (produto is null)
            return Result<ProdutoResult>.Failure("Produto não encontrado.");

        var composicao = MapearComposicao(command.Composition);

        var validacaoInsumos = await ValidarInsumosAsync(composicao, ct);
        if (validacaoInsumos.IsFailure)
            return Result<ProdutoResult>.Failure(validacaoInsumos.Error);

        var resultado = produto.Atualizar(
            nome: command.Name,
            tipoProducao: tipoProducao,
            rendimento: command.YieldAmount,
            nomeUnidade: command.YieldName,
            tempoProducaoMinutos: command.ProductionTime,
            composicao: composicao
        );

        if (resultado.IsFailure)
            return Result<ProdutoResult>.Failure(resultado.Error);

        await _produtoRepository.AtualizarAsync(produto, ct);

        return Result<ProdutoResult>.Success(await MontarAsync(produto, ct));
    }

    public async Task<Result> ExcluirAsync(ExcluirProdutoCommand command, CancellationToken ct = default)
    {
        var produto = await _produtoRepository.ObterPorIdAsync(command.Id, _currentUser.UsuarioId, ct);
        if (produto is null)
            return Result.Failure("Produto não encontrado.");

        await _produtoRepository.RemoverAsync(produto, ct);
        return Result.Success();
    }

    public async Task<Result> LimparAsync(LimparProdutosCommand command, CancellationToken ct = default)
    {
        await _produtoRepository.RemoverTodosPorUsuarioAsync(_currentUser.UsuarioId, ct);
        return Result.Success();
    }

    /* ===================== APOIO ===================== */

    private static List<(Guid InsumoId, decimal Quantidade)> MapearComposicao(
        IReadOnlyList<ItemComposicaoInput>? composicao)
        => composicao?.Select(item => (item.SupplyId, item.Amount)).ToList()
           ?? new List<(Guid, decimal)>();

    /// <summary>Garante que todo insumo da composição existe e pertence ao usuário.</summary>
    private async Task<Result> ValidarInsumosAsync(
        IReadOnlyList<(Guid InsumoId, decimal Quantidade)> composicao,
        CancellationToken ct)
    {
        if (composicao.Count == 0)
            return Result.Success();

        var ids = composicao.Select(item => item.InsumoId).Distinct().ToList();
        var encontrados = await _insumoRepository.ListarPorIdsAsync(_currentUser.UsuarioId, ids, ct);

        if (encontrados.Count != ids.Count)
            return Result.Failure("A composição referencia um insumo que não existe no seu cadastro.");

        return Result.Success();
    }

    private async Task<IReadOnlyDictionary<Guid, Insumo>> ResolverInsumosAsync(
        IEnumerable<Guid> ids,
        CancellationToken ct)
    {
        var distintos = ids.Distinct().ToList();
        if (distintos.Count == 0)
            return new Dictionary<Guid, Insumo>();

        var insumos = await _insumoRepository.ListarPorIdsAsync(_currentUser.UsuarioId, distintos, ct);
        return insumos.ToDictionary(insumo => insumo.Id);
    }

    private async Task<ProdutoResult> MontarAsync(Produto produto, CancellationToken ct)
    {
        var valorHora = await _custoRepository.ObterValorHoraAtualAsync(_currentUser.UsuarioId, ct);
        var insumos = await ResolverInsumosAsync(produto.Composicao.Select(i => i.InsumoId), ct);
        return Montar(produto, insumos, valorHora);
    }

    private static ProdutoResult Montar(
        Produto produto,
        IReadOnlyDictionary<Guid, Insumo> insumos,
        decimal valorHora)
    {
        var custoPorInsumo = insumos.ToDictionary(par => par.Key, par => par.Value.PrecoUnitario);
        var ficha = produto.Calcular(custoPorInsumo, valorHora);

        var composicao = ficha.Linhas.Select(linha =>
        {
            var existe = insumos.TryGetValue(linha.InsumoId, out var insumo);
            return new ItemComposicaoResult(
                SupplyId: linha.InsumoId,
                SupplyName: existe ? insumo!.Nome : null,
                SupplyAvailable: existe,
                Amount: linha.Quantidade,
                BaseUnit: existe ? insumo!.UnidadeBase.Codigo() : "un",
                SupplyUnitCost: linha.CustoUnitarioInsumo,
                Cost: linha.Custo
            );
        }).ToList();

        return new ProdutoResult(
            Id: produto.Id,
            Name: produto.Nome,
            ProductionType: produto.TipoProducao.Codigo(),
            YieldAmount: produto.Rendimento,
            YieldName: produto.NomeUnidade,
            ProductionTime: produto.TempoProducaoMinutos,
            Composition: composicao,
            MaterialsCost: ficha.CustoMateriais,
            LaborCost: ficha.CustoTrabalho,
            TotalCost: ficha.CustoTotal,
            UnitCost: ficha.CustoUnitario,
            HourlyRateUsed: valorHora,
            CreatedAt: produto.CriadoEm,
            UpdatedAt: produto.AtualizadoEm
        );
    }
}
