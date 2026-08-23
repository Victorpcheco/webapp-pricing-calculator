// Application/Precificacoes/Commands/AtualizarSimulacaoCommand.cs
using System.ComponentModel.DataAnnotations;

namespace Application.Precificacoes.Commands;

// Em records posicionais o atributo precisa ficar no PARÂMETRO do construtor.
// Com [property: ...] o MVC lança InvalidOperationException ao validar o modelo.
public record AtualizarSimulacaoCommand(
    Guid Id,

    [Required(ErrorMessage = "Selecione um produto para simular o preço.")]
    Guid RecipeId,

    [Range(0d, 1000d, ErrorMessage = "A margem deve estar entre 0% e 1000%.")]
    decimal Margin,

    [Range(0d, 1_000_000_000d, ErrorMessage = "O preço praticado não pode ser negativo.")]
    decimal SalePrice,

    [Range(1, 1_000_000_000, ErrorMessage = "A quantidade estimada deve ser de no mínimo 1.")]
    int Quantity
);
