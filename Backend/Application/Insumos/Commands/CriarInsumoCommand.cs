// Application/Insumos/Commands/CriarInsumoCommand.cs
using System.ComponentModel.DataAnnotations;

namespace Application.Insumos.Commands;

// Em records posicionais o atributo precisa ficar no PARÂMETRO do construtor.
// Com [property: ...] o MVC lança InvalidOperationException ao validar o modelo.
public record CriarInsumoCommand(
    [Required(ErrorMessage = "O nome do insumo é obrigatório.")]
    [MaxLength(80, ErrorMessage = "O nome deve ter no máximo 80 caracteres.")]
    string Name,

    [Required(ErrorMessage = "O tipo do insumo é obrigatório.")]
    [AllowedValues("Ingrediente", "Embalagem",
        ErrorMessage = "O tipo deve ser 'Ingrediente' ou 'Embalagem'.")]
    string Type,

    [Range(0.001, 1_000_000_000,
        ErrorMessage = "A quantidade comprada deve ser de no mínimo 0,001.")]
    decimal Quantity,

    [Required(ErrorMessage = "A unidade da compra é obrigatória.")]
    [AllowedValues("kg", "g", "L", "ml", "un",
        ErrorMessage = "A unidade deve ser kg, g, L, ml ou un.")]
    string Unit,

    [Range(0.01, 1_000_000_000,
        ErrorMessage = "O preço total pago deve ser maior que zero.")]
    decimal Price
);
