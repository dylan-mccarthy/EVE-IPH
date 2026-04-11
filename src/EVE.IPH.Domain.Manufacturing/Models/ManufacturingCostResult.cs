namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingCostResult(
    double InventionCost,
    double CopyCost,
    double TotalUsage);