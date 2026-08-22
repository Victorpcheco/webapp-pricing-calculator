namespace Domain.Entities.Users.Events;

using Domain.Common;

public record UsuarioRegistradoEvent(Guid UserId, string Email) : IDomainEvent;
