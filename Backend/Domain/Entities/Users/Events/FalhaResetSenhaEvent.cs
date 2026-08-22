namespace Domain.Entities.Users.Events;

using Domain.Common;

public record FalhaResetSenhaEvent(string Email, string Motivo) : IDomainEvent;
