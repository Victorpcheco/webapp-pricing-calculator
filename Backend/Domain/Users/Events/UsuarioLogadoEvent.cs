using Domain.Common;

namespace Domain.Users.Events;

public record UsuarioLogadoEvent(Guid UserId, string Email) : IDomainEvent;
