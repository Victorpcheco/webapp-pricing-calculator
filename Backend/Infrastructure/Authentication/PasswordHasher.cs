using Application.Authentication.Services;
using Domain.Common;

namespace WebApp.Pricing.Calculator.Infrastructure.Authentication;

public class PasswordHasher : IPasswordHasher, IScopedService
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
