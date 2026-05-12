using API.Endpoints;
using Infrastructure.DI;
using Infrastructure.Extensions;
using API.Extensions;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AutoInjectAll();

builder.Services.Configure<Infrastructure.Authentication.JwtOptions>(
    builder.Configuration.GetSection("JwtOptions"));

builder.Services.AddFrontendCors();
var app = builder.Build();

app.UseHttpsRedirection();
app.UseFrontendCors();

AuthenticationEndpoints.MapAuthenticationEndpoints(app);

app.Run();
