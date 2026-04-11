namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingUsageAllocationResult(
    double MainFacilityUsage,
    double ComponentUsage,
    double RemainingReactionUsage,
    double ReprocessingUsage,
    double TotalUsage);