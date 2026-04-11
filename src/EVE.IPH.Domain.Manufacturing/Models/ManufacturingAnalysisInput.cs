namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingAnalysisInput(
    ManufacturingPrerequisiteInput Prerequisites,
    ManufacturingFacilityUsageInput FacilityUsage,
    ManufacturingUsageAllocationInput UsageAllocation,
    ManufacturingSaleAdjustmentInput ItemSaleCharges,
    ManufacturingSaleAdjustmentInput SellExcessAdjustment,
    ManufacturingBuildBuyInput BuildBuy,
    ManufacturingActivityInput Activity,
    ComponentProductionScheduleInput ComponentSchedule,
    double BaseBlueprintProductionTimeSeconds,
    bool IsTech3,
    bool IncludeInventionCosts,
    bool IncludeCopyCosts,
    int UserRuns,
    long TotalInventedRuns,
    double PerInventionRunCost,
    double TotalCopyCost,
    double ItemMarketCost,
    double RawMaterialsCost,
    double ComponentMaterialsCost,
    double AdditionalCosts,
    bool IsBuildBuy);