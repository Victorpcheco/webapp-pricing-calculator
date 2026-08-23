// Application/Resultados/Queries/ListarResultadosQuery.cs
using System.ComponentModel.DataAnnotations;

namespace Application.Resultados.Queries;

/// <summary>Filtro de período da barra de análise da tela.</summary>
// Em records posicionais o atributo precisa ficar no PARÂMETRO do construtor.
// Com [property: ...] o MVC lança InvalidOperationException ao validar o modelo.
public record ListarResultadosQuery(
    [AllowedValues("all", "today", "week", "month", "custom",
        ErrorMessage = "O período deve ser 'all', 'today', 'week', 'month' ou 'custom'.")]
    string Periodo = "all",

    /// <summary>Só usado quando Periodo = "custom"; ignorado nos demais.</summary>
    DateTime? Inicio = null,
    DateTime? Fim = null
);
