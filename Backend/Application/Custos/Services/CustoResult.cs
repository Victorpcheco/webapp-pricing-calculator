// Application/Custos/Services/CustoResult.cs
namespace Application.Custos.Services;

public record CustoResult(
    Guid Id,
    string Description,
    DateTime CreatedAt,
    decimal Salary,
    int Hours,
    decimal Energy,
    decimal EnergyPercent,
    decimal Gas,
    decimal GasPercent,
    bool HasMei,
    decimal Das,
    decimal DepreciationRate,
    decimal EnergyReal,
    decimal GasReal,
    decimal Depreciation,
    decimal Monthly,
    decimal Hour
);
