namespace Application.Authentication.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string email, string nome);
}
