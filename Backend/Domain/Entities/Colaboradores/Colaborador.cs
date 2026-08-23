// Domain/Entities/Colaboradores/Colaborador.cs
using Domain.Common;

namespace Domain.Entities.Colaboradores;

public class Colaborador
{
    public const int CodigoTamanhoMaximo = 30;
    public const int NomeTamanhoMaximo = 80;
    public const int CargoTamanhoMaximo = 60;
    public const int TelefoneTamanhoMaximo = 20;

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }

    /// <summary>Código interno opcional informado pelo usuário (ex.: COL-01).</summary>
    public string? Codigo { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Cargo { get; private set; } = string.Empty;

    public TipoContratacao TipoContratacao { get; private set; }
    public StatusColaborador Status { get; private set; }
    public DateTime DataAdmissao { get; private set; }

    /// <summary>Salário bruto mensal (CLT) ou valor combinado (Freelancer).</summary>
    public decimal ValorBase { get; private set; }

    /// <summary>Só se aplica a Freelancer — CLT é sempre mensal.</summary>
    public FrequenciaFreelancer? FrequenciaPagamento { get; private set; }

    public string? Telefone { get; private set; }

    /// <summary>Campo calculado — armazenado para somar o custo da equipe sem recalcular linha a linha.</summary>
    public decimal CustoMensal { get; private set; }

    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    /// <summary>Encargos provisionados. Zerados para prestador de serviço.</summary>
    public ProvisaoClt Provisao => TipoContratacao == TipoContratacao.Clt
        ? ProvisaoClt.Calcular(ValorBase)
        : ProvisaoClt.Zero;

    private Colaborador() { }

    public static Result<Colaborador> Criar(
        Guid usuarioId,
        string? codigo,
        string? nome,
        string? cargo,
        TipoContratacao tipoContratacao,
        StatusColaborador status,
        DateTime? dataAdmissao,
        decimal valorBase,
        FrequenciaFreelancer? frequenciaPagamento,
        string? telefone)
    {
        try { Validar(codigo, nome, cargo, valorBase, telefone); }
        catch (ArgumentException ex) { return Result<Colaborador>.Failure(ex.Message); }

        var agora = DateTime.UtcNow;

        var colaborador = new Colaborador
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            CriadoEm = agora,
            AtualizadoEm = agora
        };

        colaborador.AplicarValores(
            codigo, nome!, cargo!, tipoContratacao, status, dataAdmissao, valorBase, frequenciaPagamento, telefone);

        return Result<Colaborador>.Success(colaborador);
    }

    public Result Atualizar(
        string? codigo,
        string? nome,
        string? cargo,
        TipoContratacao tipoContratacao,
        StatusColaborador status,
        DateTime? dataAdmissao,
        decimal valorBase,
        FrequenciaFreelancer? frequenciaPagamento,
        string? telefone)
    {
        try { Validar(codigo, nome, cargo, valorBase, telefone); }
        catch (ArgumentException ex) { return Result.Failure(ex.Message); }

        AplicarValores(
            codigo, nome!, cargo!, tipoContratacao, status, dataAdmissao, valorBase, frequenciaPagamento, telefone);
        AtualizadoEm = DateTime.UtcNow;

        return Result.Success();
    }

    private void AplicarValores(
        string? codigo,
        string nome,
        string cargo,
        TipoContratacao tipoContratacao,
        StatusColaborador status,
        DateTime? dataAdmissao,
        decimal valorBase,
        FrequenciaFreelancer? frequenciaPagamento,
        string? telefone)
    {
        Codigo = Normalizar(codigo);
        Nome = nome.Trim();
        Cargo = cargo.Trim();
        TipoContratacao = tipoContratacao;
        Status = status;

        // Sem data informada o cadastro vale a partir de hoje — mesmo default do modal
        DataAdmissao = dataAdmissao?.ToUniversalTime() ?? DateTime.UtcNow;
        ValorBase = valorBase;

        // CLT não tem forma de pagamento: o vínculo é sempre mensal.
        // Freelancer sem escolha explícita cai no default do select ("Valor fixo mensal").
        FrequenciaPagamento = tipoContratacao == TipoContratacao.Clt
            ? null
            : frequenciaPagamento ?? FrequenciaFreelancer.Mensal;

        Telefone = Normalizar(telefone);
        CustoMensal = CalcularCustoMensal();
    }

    /// <summary>
    /// CLT: salário + encargos provisionados.
    /// Freelancer mensal: o valor combinado.
    /// Freelancer por hora ou por serviço: não entra no custo fixo da equipe — o
    /// gasto depende do volume contratado no mês, então não há projeção mensal.
    /// </summary>
    private decimal CalcularCustoMensal()
    {
        if (TipoContratacao == TipoContratacao.Clt)
            return ValorBase + Provisao.Total;

        return FrequenciaPagamento == FrequenciaFreelancer.Mensal ? ValorBase : 0m;
    }

    private static void Validar(string? codigo, string? nome, string? cargo, decimal valorBase, string? telefone)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome do colaborador é obrigatório.");
        if (nome.Trim().Length > NomeTamanhoMaximo)
            throw new ArgumentException($"O nome deve ter no máximo {NomeTamanhoMaximo} caracteres.");

        if (string.IsNullOrWhiteSpace(cargo))
            throw new ArgumentException("O cargo do colaborador é obrigatório.");
        if (cargo.Trim().Length > CargoTamanhoMaximo)
            throw new ArgumentException($"O cargo deve ter no máximo {CargoTamanhoMaximo} caracteres.");

        if (codigo?.Trim().Length > CodigoTamanhoMaximo)
            throw new ArgumentException($"O código deve ter no máximo {CodigoTamanhoMaximo} caracteres.");

        if (telefone?.Trim().Length > TelefoneTamanhoMaximo)
            throw new ArgumentException($"O telefone deve ter no máximo {TelefoneTamanhoMaximo} caracteres.");

        if (valorBase <= 0)
            throw new ArgumentException("O valor base deve ser maior que zero.");
    }

    /// <summary>Campos opcionais em branco viram null — evita string vazia no banco.</summary>
    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
