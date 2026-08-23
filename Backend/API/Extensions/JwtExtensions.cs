using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace API.Extensions;

public static class JwtExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var secret = configuration["JwtOptions:Secret"]!;
        var issuer = configuration["JwtOptions:Issuer"];
        var audience = configuration["JwtOptions:Audience"];
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();

        return services;
    }
}
