namespace Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public string NomeServico { get; set; } = string.Empty;
    public string NomeEvento { get; set; } = string.Empty;
    public string DadosEvento { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}
