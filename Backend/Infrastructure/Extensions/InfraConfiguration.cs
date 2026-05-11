using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class InfraConfiguration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

        // Monta a connection string final com a senha do ambiente
        var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
        {
            Password = password
        };

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(npgsqlBuilder.ConnectionString));

        return services;
    }
}
