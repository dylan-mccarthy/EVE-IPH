namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingTimelineInput(
    double BaseBlueprintProductionTimeSeconds,
    double CopyTimeSeconds,
    double InventionTimeSeconds,
    double ComponentProductionTimeSeconds);