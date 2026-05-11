using API.Endpoints;
using Infrastructure.DI;
using Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AutoInjectAll();

builder.Services.Configure<Infrastructure.Authentication.JwtOptions>(
    builder.Configuration.GetSection("JwtOptions"));

var app = builder.Build();

app.UseHttpsRedirection();

AuthenticationEndpoints.MapAuthenticationEndpoints(app);

app.Run();
