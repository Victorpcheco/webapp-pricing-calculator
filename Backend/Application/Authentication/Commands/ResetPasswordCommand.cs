namespace Application.Authentication.Commands;

public record ResetPasswordCommand(string Email, string Token, string NovaSenha);
