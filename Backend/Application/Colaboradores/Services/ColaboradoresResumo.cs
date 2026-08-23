// Application/Colaboradores/Services/ColaboradoresResumo.cs
namespace Application.Colaboradores.Services;

/// <summary>
/// Totalizadores dos cards de estatística da tela.
/// Refletem o universo completo do usuário — nunca o recorte filtrado da busca.
/// </summary>
public record ColaboradoresResumo(
    int Total,
    int CltCount,
    int FreelancerCount,
    decimal PayrollValue
)
{
    public static readonly ColaboradoresResumo Vazio = new(0, 0, 0, 0m);
}
