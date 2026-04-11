namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingActivityResult(
    double CopyUsagePerRun,
    double InventionUsagePerRun,
    double CopyTimeSeconds,
    double InventionTimeSeconds);