namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingPrerequisiteResult(
    bool CanBuild,
    double AdvancedManufacturingTimeMultiplier);