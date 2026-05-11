namespace Domain.Entities.Users.Events;

using Domain.Common;

public record FalhaLoginUsuarioEvent(string Email, string Motivo) : IDomainEvent;
