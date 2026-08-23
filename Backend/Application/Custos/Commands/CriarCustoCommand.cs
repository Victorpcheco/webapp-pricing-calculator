// Application/Custos/Commands/CriarCustoCommand.cs
namespace Application.Custos.Commands;

public record CriarCustoCommand(
    string? Description,
    decimal Salary,
    int Hours,
    decimal Energy,
    decimal EnergyPercent,
    decimal Gas,
    decimal GasPercent,
    bool HasMei,
    decimal Das,
    decimal DepreciationRate
);
