// Application/Insumos/Queries/ListarInsumosQuery.cs
using System.ComponentModel.DataAnnotations;

namespace Application.Insumos.Queries;

/// <summary>
/// Filtros da toolbar da tela (busca por nome + select de tipo).
/// Ambos opcionais: sem filtros, retorna a lista completa do usuário.
/// </summary>
// Em records posicionais o atributo precisa ficar no PARÂMETRO do construtor.
// Com [property: ...] o MVC lança InvalidOperationException ao validar o modelo.
public record ListarInsumosQuery(
    string? Nome = null,

    [AllowedValues(null, "Ingrediente", "Embalagem",
        ErrorMessage = "O tipo deve ser 'Ingrediente' ou 'Embalagem'.")]
    string? Tipo = null
);
