namespace Application.Authentication.Commands;

public record LoginCommand(string Email, string SenhaHash);
