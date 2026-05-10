using WebApp.Pricing.Calculator.Domain.Common;

namespace WebApp.Pricing.Calculator.Domain.Users;

public class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;

    private User() { }

    public static User Create(string name, string phone, string email, string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Phone = phone,
            Email = email,
            PasswordHash = passwordHash
        };
    }
}
