// Application/Colaboradores/Commands/AtualizarColaboradorCommand.cs
using System.ComponentModel.DataAnnotations;

namespace Application.Colaboradores.Commands;

// Em records posicionais o atributo precisa ficar no PARÂMETRO do construtor.
// Com [property: ...] o MVC lança InvalidOperationException ao validar o modelo.
public record AtualizarColaboradorCommand(
    Guid Id,

    [Required(ErrorMessage = "O nome do colaborador é obrigatório.")]
    [MaxLength(80, ErrorMessage = "O nome deve ter no máximo 80 caracteres.")]
    string Name,

    [Required(ErrorMessage = "O cargo do colaborador é obrigatório.")]
    [MaxLength(60, ErrorMessage = "O cargo deve ter no máximo 60 caracteres.")]
    string Role,

    [Required(ErrorMessage = "O tipo de contratação é obrigatório.")]
    [AllowedValues("CLT", "Freelancer",
        ErrorMessage = "O tipo de contratação deve ser 'CLT' ou 'Freelancer'.")]
    string ContractType,

    [Required(ErrorMessage = "O status do colaborador é obrigatório.")]
    [AllowedValues("Ativo", "Inativo",
        ErrorMessage = "O status deve ser 'Ativo' ou 'Inativo'.")]
    string Status,

    /// <summary>Opcional — sem data informada o cadastro vale a partir de hoje.</summary>
    DateTime? AdmissionDate,

    [Range(0.01, 1_000_000_000,
        ErrorMessage = "O valor base deve ser maior que zero.")]
    decimal BaseValue,

    /// <summary>Ignorado quando o contrato é CLT.</summary>
    [AllowedValues(null, "Mensal", "Por hora", "Por serviço",
        ErrorMessage = "A forma de pagamento deve ser 'Mensal', 'Por hora' ou 'Por serviço'.")]
    string? FreelancerFrequency,

    [MaxLength(20, ErrorMessage = "O telefone deve ter no máximo 20 caracteres.")]
    string? Phone
);
