using Domain.Common;

namespace Domain.Users.Events;

public record FalhaLoginUsuarioEvent(string Email, string Reason) : IDomainEvent;
