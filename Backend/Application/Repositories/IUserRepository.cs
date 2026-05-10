using WebApp.Pricing.Calculator.Domain.Users;

namespace WebApp.Pricing.Calculator.Application.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
