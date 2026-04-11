namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingProfitabilityResult(
    double TotalRawCost,
    double TotalComponentCost,
    double TotalRawProfit,
    double TotalComponentProfit,
    double TotalRawProfitPercent,
    double TotalComponentProfitPercent,
    double TotalIskPerHourRaw,
    double TotalIskPerHourComponent);