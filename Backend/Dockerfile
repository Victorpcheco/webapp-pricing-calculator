FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar a solution e os arquivos de projeto (isso ajuda a otimizar o cache do Docker)
COPY ["WebApp.Pricing.Calculator.sln", "./"]
COPY ["API/API.csproj", "API/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Tests/Tests.csproj", "Tests/"]

# Restaurar as dependências
RUN dotnet restore

# Copiar o restante do código fonte
COPY . .

# Compilar e publicar a aplicação em modo Release
WORKDIR "/src/API"
RUN dotnet publish "API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio de runtime (utiliza uma imagem mais leve apenas com o necessário para rodar)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# O Render geralmente expõe a porta através da variável de ambiente PORT, 
# e o ASP.NET Core 8+ escuta na 8080 por padrão
EXPOSE 8080

ENTRYPOINT ["dotnet", "API.dll"]
