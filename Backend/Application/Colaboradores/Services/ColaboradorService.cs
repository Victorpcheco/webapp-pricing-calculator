// Application/Colaboradores/Services/ColaboradorService.cs
using Application.Colaboradores.Commands;
using Application.Colaboradores.Queries;
using Application.Common;
using Application.Repositories;
using Domain.Common;
using Domain.Entities.Colaboradores;

namespace Application.Colaboradores.Services;

public class ColaboradorService : IScopedService
{
    private const string ErroTipoContratacao = "O tipo de contratação deve ser 'CLT' ou 'Freelancer'.";
    private const string ErroStatus = "O status deve ser 'Ativo' ou 'Inativo'.";
    private const string ErroFrequencia = "A forma de pagamento deve ser 'Mensal', 'Por hora' ou 'Por serviço'.";

    private readonly IColaboradorRepository _colaboradorRepository;
    private readonly ICurrentUserService _currentUser;

    public ColaboradorService(IColaboradorRepository colaboradorRepository, ICurrentUserService currentUser)
    {
        _colaboradorRepository = colaboradorRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<ColaboradoresListResult>> ListarAsync(
        ListarColaboradoresQuery query,
        CancellationToken ct = default)
    {
        TipoContratacao? tipo = null;
        if (!string.IsNullOrWhiteSpace(query.Tipo))
        {
            if (!TipoContratacaoExtensions.TryParse(query.Tipo, out var tipoFiltro))
                return Result<ColaboradoresListResult>.Failure(ErroTipoContratacao);

            tipo = tipoFiltro;
        }

        var colaboradores = await _colaboradorRepository.ListarPorUsuarioAsync(
            _currentUser.UsuarioId, query.Busca, tipo, ct);
        var resumo = await _colaboradorRepository.ObterResumoAsync(_currentUser.UsuarioId, ct);

        var dados = colaboradores.Select(ToResult).ToList();
        return Result<ColaboradoresListResult>.Success(new ColaboradoresListResult(dados, resumo));
    }

    public async Task<Result<ColaboradorResult>> CriarAsync(
        CriarColaboradorCommand command,
        CancellationToken ct = default)
    {
        var entrada = InterpretarTokens(command.ContractType, command.Status, command.FreelancerFrequency);
        if (entrada.IsFailure)
            return Result<ColaboradorResult>.Failure(entrada.Error);

        var (tipo, status, frequencia) = entrada.Value;

        // Código sequencial gerado pelo backend — o usuário não escolhe o próprio código.
        var totalExistente = await _colaboradorRepository.ContarPorUsuarioAsync(_currentUser.UsuarioId, ct);
        var codigo = $"COL-{totalExistente + 1:D3}";

        var resultado = Colaborador.Criar(
            usuarioId: _currentUser.UsuarioId,
            codigo: codigo,
            nome: command.Name,
            cargo: command.Role,
            tipoContratacao: tipo,
            status: status,
            dataAdmissao: command.AdmissionDate,
            valorBase: command.BaseValue,
            frequenciaPagamento: frequencia,
            telefone: command.Phone
        );

        if (resultado.IsFailure)
            return Result<ColaboradorResult>.Failure(resultado.Error);

        await _colaboradorRepository.AdicionarAsync(resultado.Value, ct);
        return Result<ColaboradorResult>.Success(ToResult(resultado.Value));
    }

    public async Task<Result<ColaboradorResult>> AtualizarAsync(
        AtualizarColaboradorCommand command,
        CancellationToken ct = default)
    {
        var entrada = InterpretarTokens(command.ContractType, command.Status, command.FreelancerFrequency);
        if (entrada.IsFailure)
            return Result<ColaboradorResult>.Failure(entrada.Error);

        var (tipo, status, frequencia) = entrada.Value;

        var colaborador = await _colaboradorRepository.ObterPorIdAsync(command.Id, _currentUser.UsuarioId, ct);
        if (colaborador is null)
            return Result<ColaboradorResult>.Failure("Colaborador não encontrado.");

        var resultado = colaborador.Atualizar(
            // Código é imutável após o cadastro — não é reatribuído pelo usuário na edição.
            codigo: colaborador.Codigo,
            nome: command.Name,
            cargo: command.Role,
            tipoContratacao: tipo,
            status: status,
            dataAdmissao: command.AdmissionDate,
            valorBase: command.BaseValue,
            frequenciaPagamento: frequencia,
            telefone: command.Phone
        );

        if (resultado.IsFailure)
            return Result<ColaboradorResult>.Failure(resultado.Error);

        await _colaboradorRepository.AtualizarAsync(colaborador, ct);
        return Result<ColaboradorResult>.Success(ToResult(colaborador));
    }

    public async Task<Result> ExcluirAsync(ExcluirColaboradorCommand command, CancellationToken ct = default)
    {
        var colaborador = await _colaboradorRepository.ObterPorIdAsync(command.Id, _currentUser.UsuarioId, ct);
        if (colaborador is null)
            return Result.Failure("Colaborador não encontrado.");

        await _colaboradorRepository.RemoverAsync(colaborador, ct);
        return Result.Success();
    }

    public async Task<Result> LimparAsync(LimparColaboradoresCommand command, CancellationToken ct = default)
    {
        await _colaboradorRepository.RemoverTodosPorUsuarioAsync(_currentUser.UsuarioId, ct);
        return Result.Success();
    }

    /// <summary>Traduz os tokens do frontend para os enums do domínio de uma só vez.</summary>
    private static Result<(TipoContratacao Tipo, StatusColaborador Status, FrequenciaFreelancer? Frequencia)>
        InterpretarTokens(string? contractType, string? status, string? freelancerFrequency)
    {
        if (!TipoContratacaoExtensions.TryParse(contractType, out var tipo))
            return Result<(TipoContratacao, StatusColaborador, FrequenciaFreelancer?)>.Failure(ErroTipoContratacao);

        if (!StatusColaboradorExtensions.TryParse(status, out var statusColaborador))
            return Result<(TipoContratacao, StatusColaborador, FrequenciaFreelancer?)>.Failure(ErroStatus);

        FrequenciaFreelancer? frequencia = null;
        if (!string.IsNullOrWhiteSpace(freelancerFrequency))
        {
            if (!FrequenciaFreelancerExtensions.TryParse(freelancerFrequency, out var frequenciaInformada))
                return Result<(TipoContratacao, StatusColaborador, FrequenciaFreelancer?)>.Failure(ErroFrequencia);

            frequencia = frequenciaInformada;
        }

        return Result<(TipoContratacao, StatusColaborador, FrequenciaFreelancer?)>
            .Success((tipo, statusColaborador, frequencia));
    }

    private static ColaboradorResult ToResult(Colaborador c)
    {
        var provisao = c.Provisao;

        return new ColaboradorResult(
            Id: c.Id,
            Code: c.Codigo,
            Name: c.Nome,
            Role: c.Cargo,
            ContractType: c.TipoContratacao.Codigo(),
            Status: c.Status.Codigo(),
            AdmissionDate: c.DataAdmissao,
            BaseValue: c.ValorBase,
            FreelancerFrequency: c.FrequenciaPagamento?.Codigo(),
            Phone: c.Telefone,
            Charges: new EncargosResult(
                provisao.Fgts,
                provisao.DecimoTerceiro,
                provisao.Ferias,
                provisao.UmTercoFerias,
                provisao.Total
            ),
            MonthlyCost: c.CustoMensal,
            CreatedAt: c.CriadoEm,
            UpdatedAt: c.AtualizadoEm
        );
    }
}
