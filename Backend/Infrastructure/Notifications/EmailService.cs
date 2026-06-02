using Application.Common;
using Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Notifications;

public class EmailService : IEmailService, IScopedService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailOptions _emailOptions;

    public EmailService(ILogger<EmailService> logger, IOptions<EmailOptions> emailOptions)
    {
        _logger = logger;
        _emailOptions = emailOptions.Value;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_emailOptions.SmtpServer))
        {
            _logger.LogWarning("Configurações de E-mail não informadas. Simulando envio para {toEmail} com token {token}", toEmail, token);
            return;
        }

        try
        {
            using var client = new SmtpClient(_emailOptions.SmtpServer, _emailOptions.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailOptions.SmtpUsername, _emailOptions.SmtpPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailOptions.FromEmail, _emailOptions.FromName),
                Subject = "Recuperação de Senha",
                Body = $"<h1>Recuperação de Senha</h1><p>Utilize este código para redefinir sua senha: <strong>{token}</strong></p><p>Este código expira em 15 minutos.</p>",
                IsBodyHtml = true
            };
            
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("E-mail de recuperação de senha enviado com sucesso para {toEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar e-mail de recuperação de senha para {toEmail}", toEmail);
            throw; // Opcional: relançar a exceção para que o frontend receba erro e saiba que o e-mail não chegou
        }
    }
}
