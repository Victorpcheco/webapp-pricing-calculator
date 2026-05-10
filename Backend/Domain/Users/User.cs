using Domain.Common;

namespace Domain.Users;

public class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;

    private User() { }

    public static Result<User> Create(string name, string phone, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name) || 
            string.IsNullOrWhiteSpace(phone) || 
            string.IsNullOrWhiteSpace(email) || 
            string.IsNullOrWhiteSpace(passwordHash))
        {
            return Result<User>.Failure("Dados inválidos");
        }

        if (!email.Contains("@") || !email.Contains("."))
        {
            return Result<User>.Failure("E-mail inválido");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Phone = phone,
            Email = email,
            PasswordHash = passwordHash
        };

        return Result<User>.Success(user);
    }
}
