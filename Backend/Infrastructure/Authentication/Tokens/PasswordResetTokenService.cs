using Application.Authentication.Services;
using Domain.Common;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Authentication.Tokens;

public class PasswordResetTokenService : IPasswordResetTokenService, IScopedService
{
    private readonly IMemoryCache _memoryCache;

    public PasswordResetTokenService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<string> GenerateTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Gera um token de 6 dígitos
        var random = new Random();
        var token = random.Next(100000, 999999).ToString();

        // Expira em 15 minutos
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

        // Usa o userId como parte da chave
        var cacheKey = $"PasswordResetToken_{userId}";
        _memoryCache.Set(cacheKey, token, cacheOptions);

        return Task.FromResult(token);
    }

    public Task<bool> ValidateTokenAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"PasswordResetToken_{userId}";

        if (!_memoryCache.TryGetValue(cacheKey, out string? cachedToken))
        {
            // Se não encontrou, ou nunca foi gerado ou expirou
            throw new ArgumentException("Código expirado ou inválido.");
        }

        if (cachedToken == token)
        {
            // Token válido, removemos para não ser usado novamente
            _memoryCache.Remove(cacheKey);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
