namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingCostInput(
    bool IsTech3,
    bool IncludeInventionCosts,
    bool IncludeCopyCosts,
    int UserRuns,
    long TotalInventedRuns,
    double PerInventionRunCost,
    double TotalCopyCost,
    double MainFacilityUsage,
    double ComponentUsage,
    double InventionUsage,
    double CopyUsage,
    double RemainingReactionUsage,
    double ReprocessingUsage);