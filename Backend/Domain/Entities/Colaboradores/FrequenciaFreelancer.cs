// Domain/Entities/Colaboradores/FrequenciaFreelancer.cs
namespace Domain.Entities.Colaboradores;

/// <summary>Forma de pagamento combinada com o prestador. Não se aplica a CLT.</summary>
public enum FrequenciaFreelancer
{
    Mensal = 1,
    PorHora = 2,
    PorServico = 3
}

public static class FrequenciaFreelancerExtensions
{
    /// <summary>Token exato trafegado com o frontend ('Mensal', 'Por hora', 'Por serviço').</summary>
    public static string Codigo(this FrequenciaFreelancer frequencia) => frequencia switch
    {
        FrequenciaFreelancer.Mensal => "Mensal",
        FrequenciaFreelancer.PorHora => "Por hora",
        FrequenciaFreelancer.PorServico => "Por serviço",
        _ => throw new ArgumentOutOfRangeException(nameof(frequencia))
    };

    public static bool TryParse(string? valor, out FrequenciaFreelancer frequencia)
    {
        switch (valor?.Trim())
        {
            case "Mensal":
                frequencia = FrequenciaFreelancer.Mensal;
                return true;
            case "Por hora":
                frequencia = FrequenciaFreelancer.PorHora;
                return true;
            case "Por serviço":
                frequencia = FrequenciaFreelancer.PorServico;
                return true;
            default:
                frequencia = default;
                return false;
        }
    }
}
