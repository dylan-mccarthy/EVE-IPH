namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingFacilityUsageResult(
    double FacilityUsage,
    double MainFacilityUsage,
    double ManufacturingFacilityUsage,
    double ReactionFacilityUsage,
    double TotalReactionFacilityUsageDelta);