// Application/Custos/Services/CustoService.cs
using Application.Common;
using Application.Custos.Commands;
using Application.Custos.Queries;
using Application.Repositories;
using Domain.Common;
using Domain.Entities.Custos;

namespace Application.Custos.Services;

public class CustoService : IScopedService
{
    private readonly ICustoRepository _custoRepository;
    private readonly ICurrentUserService _currentUser;

    public CustoService(ICustoRepository custoRepository, ICurrentUserService currentUser)
    {
        _custoRepository = custoRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CustoResult>> ListarAsync(ListarCustosQuery query, CancellationToken ct = default)
    {
        var custos = await _custoRepository.ListarPorUsuarioAsync(_currentUser.UsuarioId, ct);
        return custos.Select(ToResult).ToList();
    }

    public async Task<Result<CustoResult>> CriarAsync(CriarCustoCommand command, CancellationToken ct = default)
    {
        var resultado = CustoOperacional.Criar(
            usuarioId: _currentUser.UsuarioId,
            descricao: command.Description,
            proLabore: command.Salary,
            horasMensais: command.Hours,
            contaEnergia: command.Energy,
            percentualEnergiaTrabalho: command.EnergyPercent,
            gastoGas: command.Gas,
            percentualGasTrabalho: command.GasPercent,
            possuiMei: command.HasMei,
            valorDas: command.Das,
            taxaDepreciacao: command.DepreciationRate
        );

        if (resultado.IsFailure)
            return Result<CustoResult>.Failure(resultado.Error);

        await _custoRepository.AdicionarAsync(resultado.Value, ct);
        return Result<CustoResult>.Success(ToResult(resultado.Value));
    }

    public async Task<Result<CustoResult>> AtualizarAsync(AtualizarCustoCommand command, CancellationToken ct = default)
    {
        var custo = await _custoRepository.ObterPorIdAsync(command.Id, _currentUser.UsuarioId, ct);
        if (custo is null)
            return Result<CustoResult>.Failure("Configuração de custo não encontrada.");

        var resultado = custo.Atualizar(
            proLabore: command.Salary,
            horasMensais: command.Hours,
            contaEnergia: command.Energy,
            percentualEnergiaTrabalho: command.EnergyPercent,
            gastoGas: command.Gas,
            percentualGasTrabalho: command.GasPercent,
            possuiMei: command.HasMei,
            valorDas: command.Das,
            taxaDepreciacao: command.DepreciationRate
        );

        if (resultado.IsFailure)
            return Result<CustoResult>.Failure(resultado.Error);

        await _custoRepository.AtualizarAsync(custo, ct);
        return Result<CustoResult>.Success(ToResult(custo));
    }

    public async Task<Result> ExcluirAsync(ExcluirCustoCommand command, CancellationToken ct = default)
    {
        var custo = await _custoRepository.ObterPorIdAsync(command.Id, _currentUser.UsuarioId, ct);
        if (custo is null)
            return Result.Failure("Configuração de custo não encontrada.");

        await _custoRepository.RemoverAsync(custo, ct);
        return Result.Success();
    }

    public async Task<Result> LimparHistoricoAsync(LimparHistoricoCustosCommand command, CancellationToken ct = default)
    {
        await _custoRepository.RemoverTodosPorUsuarioAsync(_currentUser.UsuarioId, ct);
        return Result.Success();
    }

    private static CustoResult ToResult(CustoOperacional c) => new(
        Id: c.Id,
        Description: c.Descricao,
        CreatedAt: c.CriadoEm,
        Salary: c.ProLabore,
        Hours: c.HorasMensais,
        Energy: c.ContaEnergia,
        EnergyPercent: c.PercentualEnergiaTrabalho,
        Gas: c.GastoGas,
        GasPercent: c.PercentualGasTrabalho,
        HasMei: c.PossuiMei,
        Das: c.ValorDas,
        DepreciationRate: c.TaxaDepreciacao,
        EnergyReal: c.EnergiaReal,
        GasReal: c.GasReal,
        Depreciation: c.ValorDepreciacao,
        Monthly: c.CustoMensal,
        Hour: c.ValorHora
    );
}
