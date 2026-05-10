namespace Infrastructure.Persistence.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
