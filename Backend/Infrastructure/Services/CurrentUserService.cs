// Infrastructure/Services/CurrentUserService.cs
using Application.Common;
using Domain.Common;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public class CurrentUserService : ICurrentUserService, IScopedService
{
    public Guid UsuarioId { get; }

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        var claim = httpContextAccessor.HttpContext?.User.Claims
            .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        UsuarioId = Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
