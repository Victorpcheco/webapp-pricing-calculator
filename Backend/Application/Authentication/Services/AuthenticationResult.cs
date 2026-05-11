namespace Application.Authentication.Services;

public record AuthenticationResult(Guid UserId, string Token);
