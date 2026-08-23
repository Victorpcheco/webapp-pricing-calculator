// Domain/Entities/Colaboradores/StatusColaborador.cs
namespace Domain.Entities.Colaboradores;

public enum StatusColaborador
{
    Ativo = 1,
    Inativo = 2
}

public static class StatusColaboradorExtensions
{
    /// <summary>Token exato trafegado com o frontend (select de status do modal).</summary>
    public static string Codigo(this StatusColaborador status) => status switch
    {
        StatusColaborador.Ativo => "Ativo",
        StatusColaborador.Inativo => "Inativo",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParse(string? valor, out StatusColaborador status)
    {
        switch (valor?.Trim())
        {
            case "Ativo":
                status = StatusColaborador.Ativo;
                return true;
            case "Inativo":
                status = StatusColaborador.Inativo;
                return true;
            default:
                status = default;
                return false;
        }
    }
}
