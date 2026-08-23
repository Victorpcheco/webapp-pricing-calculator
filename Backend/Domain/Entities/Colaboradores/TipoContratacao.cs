// Domain/Entities/Colaboradores/TipoContratacao.cs
namespace Domain.Entities.Colaboradores;

public enum TipoContratacao
{
    Clt = 1,
    Freelancer = 2
}

public static class TipoContratacaoExtensions
{
    /// <summary>Token exato esperado pelo frontend (item.contractType === 'CLT').</summary>
    public static string Codigo(this TipoContratacao tipo) => tipo switch
    {
        TipoContratacao.Clt => "CLT",
        TipoContratacao.Freelancer => "Freelancer",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    public static bool TryParse(string? valor, out TipoContratacao tipo)
    {
        switch (valor?.Trim())
        {
            case "CLT":
                tipo = TipoContratacao.Clt;
                return true;
            case "Freelancer":
                tipo = TipoContratacao.Freelancer;
                return true;
            default:
                tipo = default;
                return false;
        }
    }
}
