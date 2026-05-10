using Domain.Common;

namespace Domain.Users.Events;

public record UsuarioRegistradoEvent(Guid UserId, string Email) : IDomainEvent;
