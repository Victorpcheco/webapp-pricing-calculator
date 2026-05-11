using System.Text.Json;
using Application.Common;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Events;

public class EventPublisher : IEventPublisher, IScopedService
{
    private readonly ILogger<EventPublisher> _logger;
    private readonly AppDbContext _context;

    public EventPublisher(ILogger<EventPublisher> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : IDomainEvent
    {
        // 1. Pegamos o nome do evento (ex: "FalhaLoginUsuarioEvent")
        var nomeEvento = domainEvent.GetType().Name;

        // 2. Transformamos o objeto do evento em um JSON bonitinho
        var dadosEvento = JsonSerializer.Serialize(domainEvent);

        // 3. Gravamos o Log Estruturado no Console
        _logger.LogInformation("AUDITORIA [{EventName}]: {EventData}", nomeEvento, dadosEvento);

        // 4. Salvamos na Tabela Central de Auditoria
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            NomeServico = "AuthenticationService",
            NomeEvento = nomeEvento,
            DadosEvento = dadosEvento,
            DataCriacao = DateTime.UtcNow
        };

        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
