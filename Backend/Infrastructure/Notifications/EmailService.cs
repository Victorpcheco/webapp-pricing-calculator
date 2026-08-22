using Application.Common;
using Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace Infrastructure.Notifications;

public class EmailService : IEmailService, IScopedService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailOptions _emailOptions;
    private static readonly HttpClient _httpClient = new HttpClient();

    public EmailService(ILogger<EmailService> logger, IOptions<EmailOptions> emailOptions)
    {
        _logger = logger;
        _emailOptions = emailOptions.Value;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_emailOptions.ResendApiKey))
        {
            _logger.LogWarning("Chave do Resend não informada (EmailOptions__ResendApiKey). Simulando envio para {toEmail} com token {token}", toEmail, token);
            return;
        }

        try
        {
            var requestBody = new
            {
                from = $"{_emailOptions.FromName} <{_emailOptions.FromEmail}>",
                to = new[] { toEmail },
                subject = "Recuperação de Senha",
                html = $"<h1>Recuperação de Senha</h1><p>Utilize este código para redefinir sua senha: <strong>{token}</strong></p><p>Este código expira em 15 minutos.</p>"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _emailOptions.ResendApiKey);
            request.Content = JsonContent.Create(requestBody);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("E-mail de recuperação de senha enviado via Resend com sucesso para {toEmail}", toEmail);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Falha ao enviar e-mail via Resend para {toEmail}. StatusCode: {StatusCode}. Response: {Response}", toEmail, response.StatusCode, errorContent);
                throw new Exception($"Resend API error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar e-mail de recuperação de senha para {toEmail}", toEmail);
            throw;
        }
    }
}
