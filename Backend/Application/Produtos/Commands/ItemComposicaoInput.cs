// Application/Produtos/Commands/ItemComposicaoInput.cs
using System.ComponentModel.DataAnnotations;

namespace Application.Produtos.Commands;

// Em records posicionais o atributo precisa ficar no PARÂMETRO do construtor.
// Com [property: ...] o MVC lança InvalidOperationException ao validar o modelo.
public record ItemComposicaoInput(
    [Required(ErrorMessage = "Selecione o insumo do item da composição.")]
    Guid SupplyId,

    [Range(0.000001, 1_000_000_000,
        ErrorMessage = "A quantidade usada deve ser maior que zero.")]
    decimal Amount
);
