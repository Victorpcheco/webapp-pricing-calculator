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
        var password = Environment.GetEnvironmentVariable("password");
        var host = Environment.GetEnvironmentVariable("host");
        var port = Environment.GetEnvironmentVariable("port");
        var database = Environment.GetEnvironmentVariable("database");
        var username = Environment.GetEnvironmentVariable("username");

        // Monta a connection string final com a senha do ambiente
        var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
        {
            Username = username,
            Host = host,
            Port = int.TryParse(port, out var portNumber) ? portNumber : 5432,
            Database = database,
            Password = password
        };

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(npgsqlBuilder.ConnectionString));

        return services;
    }
}
