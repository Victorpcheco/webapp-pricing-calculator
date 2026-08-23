// Application/Produtos/Commands/CriarProdutoCommand.cs
using System.ComponentModel.DataAnnotations;

namespace Application.Produtos.Commands;

// Em records posicionais o atributo precisa ficar no PARÂMETRO do construtor.
// Com [property: ...] o MVC lança InvalidOperationException ao validar o modelo.
public record CriarProdutoCommand(
    [Required(ErrorMessage = "O nome do produto é obrigatório.")]
    [MaxLength(80, ErrorMessage = "O nome deve ter no máximo 80 caracteres.")]
    string Name,

    [Required(ErrorMessage = "O tipo de produção é obrigatório.")]
    [AllowedValues("Produto inteiro", "Porções",
        ErrorMessage = "O tipo de produção deve ser 'Produto inteiro' ou 'Porções'.")]
    string ProductionType,

    [Range(1, 1_000_000, ErrorMessage = "O rendimento deve ser de no mínimo 1.")]
    int YieldAmount,

    [Required(ErrorMessage = "O nome da unidade é obrigatório.")]
    [MaxLength(30, ErrorMessage = "O nome da unidade deve ter no máximo 30 caracteres.")]
    string YieldName,

    [Range(0, 100_000, ErrorMessage = "O tempo de produção não pode ser negativo.")]
    int ProductionTime,

    IReadOnlyList<ItemComposicaoInput>? Composition
);
