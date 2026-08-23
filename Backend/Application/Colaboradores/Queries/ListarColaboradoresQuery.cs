// Application/Colaboradores/Queries/ListarColaboradoresQuery.cs
using System.ComponentModel.DataAnnotations;

namespace Application.Colaboradores.Queries;

/// <summary>
/// Filtros da toolbar da tela (busca por nome ou cargo + select de contratação).
/// Ambos opcionais: sem filtros, retorna a equipe completa do usuário.
/// </summary>
// Em records posicionais o atributo precisa ficar no PARÂMETRO do construtor.
// Com [property: ...] o MVC lança InvalidOperationException ao validar o modelo.
public record ListarColaboradoresQuery(
    /// <summary>Termo único aplicado a nome E cargo, como o campo de busca da tela.</summary>
    string? Busca = null,

    [AllowedValues(null, "CLT", "Freelancer",
        ErrorMessage = "O tipo de contratação deve ser 'CLT' ou 'Freelancer'.")]
    string? Tipo = null
);
