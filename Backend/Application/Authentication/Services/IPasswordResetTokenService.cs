namespace Application.Authentication.Services;

public interface IPasswordResetTokenService
{
    Task<string> GenerateTokenAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(Guid userId, string token, CancellationToken cancellationToken = default);
}
