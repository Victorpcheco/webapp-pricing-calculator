namespace Application.Authentication.Commands;

public record RegisterUserCommand(string Nome, string Telefone, string Email, string SenhaHash);
