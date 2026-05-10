namespace Application.Authentication.Commands;

public record RegisterUserCommand(string Name, string Phone, string Email, string Password);
