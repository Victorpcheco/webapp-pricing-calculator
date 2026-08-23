// Application/Colaboradores/Services/ColaboradorResult.cs
namespace Application.Colaboradores.Services;

/// <summary>
/// Encargos provisionados de um colaborador CLT.
/// Espelha a interface CltCharges do frontend — zerado para Freelancer.
/// </summary>
public record EncargosResult(
    decimal Fgts,
    decimal DecimoTerceiro,
    decimal Ferias,
    decimal UmTercoFerias,
    decimal Total
);

/// <summary>Uma linha da tabela de colaboradores, já com os encargos calculados pelo backend.</summary>
public record ColaboradorResult(
    Guid Id,
    string? Code,
    string Name,
    string Role,
    string ContractType,
    string Status,
    DateTime AdmissionDate,
    decimal BaseValue,
    /// <summary>null quando o contrato é CLT.</summary>
    string? FreelancerFrequency,
    string? Phone,
    EncargosResult Charges,
    decimal MonthlyCost,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
