// Application/Custos/Commands/AtualizarCustoCommand.cs
namespace Application.Custos.Commands;

public record AtualizarCustoCommand(
    Guid Id,
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
