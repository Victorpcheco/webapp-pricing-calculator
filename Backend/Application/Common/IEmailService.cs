namespace Application.Common;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string token, CancellationToken cancellationToken = default);
}
