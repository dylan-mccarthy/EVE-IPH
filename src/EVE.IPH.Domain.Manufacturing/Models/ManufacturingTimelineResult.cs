namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingTimelineResult(
    double BlueprintProductionTimeSeconds,
    double TotalProductionTimeSeconds);