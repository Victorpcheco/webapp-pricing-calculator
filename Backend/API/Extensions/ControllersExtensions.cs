namespace API.Extensions;

public static class ControllersExtensions
{
    public static IServiceCollection AddControllersConfiguration(this IServiceCollection services)
    {
        services.AddControllers();
        return services;
    }

    public static IApplicationBuilder UseControllersConfiguration(this IApplicationBuilder app)
    {
        if (app is WebApplication webApp)
        {
            webApp.MapControllers();
        }
        else if (app is IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllers();
        }
        return app;
    }
}
