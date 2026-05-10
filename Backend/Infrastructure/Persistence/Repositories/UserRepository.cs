using Microsoft.EntityFrameworkCore;
using WebApp.Pricing.Calculator.Application.Repositories;
using WebApp.Pricing.Calculator.Domain.Users;

namespace WebApp.Pricing.Calculator.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository, IScopedService
{
    private readonly global::Infrastructure.Data.AppDbContext _context;

    public UserRepository(global::Infrastructure.Data.AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
    }
}
