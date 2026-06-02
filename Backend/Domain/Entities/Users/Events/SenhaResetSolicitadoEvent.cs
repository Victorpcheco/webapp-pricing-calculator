namespace Domain.Entities.Users.Events;

using Domain.Common;

public record SenhaResetSolicitadoEvent(string Email) : IDomainEvent;
