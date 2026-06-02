namespace Domain.Entities.Users.Events;

using Domain.Common;

public record SenhaResetConcluidoEvent(Guid UserId, string Email) : IDomainEvent;
