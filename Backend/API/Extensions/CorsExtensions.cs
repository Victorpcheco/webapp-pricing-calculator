namespace API.Extensions;

public static class CorsExtensions
{
    private const string PolicyName = "AllowFrontend";

    public static IServiceCollection AddFrontendCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy.WithOrigins("http://localhost:4200")
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }

    public static IApplicationBuilder UseFrontendCors(this IApplicationBuilder app)
    {
        return app.UseCors(PolicyName);
    }
}
