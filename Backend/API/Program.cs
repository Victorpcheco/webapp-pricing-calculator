using Infrastructure.DI.Shared;
using Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Carrega as variáveis do .env subindo na árvore de diretórios
DotNetEnv.Env.TraversePath().Load();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AutoInjectAll();

var app = builder.Build();

app.UseHttpsRedirection();
app.Run();
