using Infrastructure.DI;
using Infrastructure.Extensions;
using API.Extensions;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllersConfiguration();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AutoInjectAll();
builder.Services.AddMemoryCache();

builder.Services.Configure<Infrastructure.Authentication.JwtOptions>(
    builder.Configuration.GetSection("JwtOptions"));

builder.Services.Configure<Infrastructure.Notifications.EmailOptions>(
    builder.Configuration.GetSection("EmailOptions"));

builder.Services.AddFrontendCors();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseFrontendCors();

app.UseControllersConfiguration();

app.Run();
