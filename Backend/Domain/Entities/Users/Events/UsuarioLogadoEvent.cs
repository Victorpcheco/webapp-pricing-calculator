namespace Domain.Entities.Users.Events;

using Domain.Common;

public record UsuarioLogadoEvent(Guid UserId, string Email) : IDomainEvent;
