var builder = WebApplication.CreateBuilder(args);

builder.AutoInjectAll();

var app = builder.Build();

app.UseHttpsRedirection();
app.Run();
