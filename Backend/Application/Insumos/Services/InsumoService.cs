// Application/Insumos/Services/InsumoService.cs
using Application.Common;
using Application.Insumos.Commands;
using Application.Insumos.Queries;
using Application.Repositories;
using Domain.Common;
using Domain.Entities.Insumos;

namespace Application.Insumos.Services;

public class InsumoService : IScopedService
{
    private readonly IInsumoRepository _insumoRepository;
    private readonly ICurrentUserService _currentUser;

    public InsumoService(IInsumoRepository insumoRepository, ICurrentUserService currentUser)
    {
        _insumoRepository = insumoRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<InsumosListResult>> ListarAsync(ListarInsumosQuery query, CancellationToken ct = default)
    {
        TipoInsumo? tipo = null;
        if (!string.IsNullOrWhiteSpace(query.Tipo))
        {
            if (!TipoInsumoExtensions.TryParse(query.Tipo, out var tipoFiltro))
                return Result<InsumosListResult>.Failure("O tipo deve ser 'Ingrediente' ou 'Embalagem'.");

            tipo = tipoFiltro;
        }

        var insumos = await _insumoRepository.ListarPorUsuarioAsync(_currentUser.UsuarioId, query.Nome, tipo, ct);
        var resumo = await _insumoRepository.ObterResumoAsync(_currentUser.UsuarioId, ct);

        var dados = insumos.Select(ToResult).ToList();
        return Result<InsumosListResult>.Success(new InsumosListResult(dados, resumo));
    }

    public async Task<Result<InsumoResult>> CriarAsync(CriarInsumoCommand command, CancellationToken ct = default)
    {
        if (!TipoInsumoExtensions.TryParse(command.Type, out var tipo))
            return Result<InsumoResult>.Failure("O tipo deve ser 'Ingrediente' ou 'Embalagem'.");

        if (!UnidadeMedidaExtensions.TryParse(command.Unit, out var unidade))
            return Result<InsumoResult>.Failure("A unidade deve ser kg, g, L, ml ou un.");

        var resultado = Insumo.Criar(
            usuarioId: _currentUser.UsuarioId,
            nome: command.Name,
            tipo: tipo,
            quantidade: command.Quantity,
            unidade: unidade,
            preco: command.Price
        );

        if (resultado.IsFailure)
            return Result<InsumoResult>.Failure(resultado.Error);

        await _insumoRepository.AdicionarAsync(resultado.Value, ct);
        return Result<InsumoResult>.Success(ToResult(resultado.Value));
    }

    public async Task<Result<InsumoResult>> AtualizarAsync(AtualizarInsumoCommand command, CancellationToken ct = default)
    {
        if (!TipoInsumoExtensions.TryParse(command.Type, out var tipo))
            return Result<InsumoResult>.Failure("O tipo deve ser 'Ingrediente' ou 'Embalagem'.");

        if (!UnidadeMedidaExtensions.TryParse(command.Unit, out var unidade))
            return Result<InsumoResult>.Failure("A unidade deve ser kg, g, L, ml ou un.");

        var insumo = await _insumoRepository.ObterPorIdAsync(command.Id, _currentUser.UsuarioId, ct);
        if (insumo is null)
            return Result<InsumoResult>.Failure("Insumo não encontrado.");

        var resultado = insumo.Atualizar(
            nome: command.Name,
            tipo: tipo,
            quantidade: command.Quantity,
            unidade: unidade,
            preco: command.Price
        );

        if (resultado.IsFailure)
            return Result<InsumoResult>.Failure(resultado.Error);

        await _insumoRepository.AtualizarAsync(insumo, ct);
        return Result<InsumoResult>.Success(ToResult(insumo));
    }

    public async Task<Result> ExcluirAsync(ExcluirInsumoCommand command, CancellationToken ct = default)
    {
        var insumo = await _insumoRepository.ObterPorIdAsync(command.Id, _currentUser.UsuarioId, ct);
        if (insumo is null)
            return Result.Failure("Insumo não encontrado.");

        await _insumoRepository.RemoverAsync(insumo, ct);
        return Result.Success();
    }

    public async Task<Result> LimparAsync(LimparInsumosCommand command, CancellationToken ct = default)
    {
        await _insumoRepository.RemoverTodosPorUsuarioAsync(_currentUser.UsuarioId, ct);
        return Result.Success();
    }

    private static InsumoResult ToResult(Insumo i) => new(
        Id: i.Id,
        Name: i.Nome,
        Type: i.Tipo.Codigo(),
        Quantity: i.Quantidade,
        Unit: i.Unidade.Codigo(),
        Price: i.Preco,
        UnitCost: i.PrecoUnitario,
        BaseQuantity: i.QuantidadeBase,
        BaseUnit: i.UnidadeBase.Codigo(),
        CreatedAt: i.CriadoEm,
        UpdatedAt: i.AtualizadoEm
    );
}
