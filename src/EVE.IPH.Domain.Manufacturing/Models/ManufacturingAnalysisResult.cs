namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingAnalysisResult(
    ManufacturingPrerequisiteResult Prerequisites,
    ManufacturingFacilityUsageResult FacilityUsage,
    ManufacturingUsageAllocationResult UsageAllocation,
    ManufacturingSaleAdjustmentResult ItemSaleCharges,
    ManufacturingSaleAdjustmentResult SellExcessAdjustment,
    ManufacturingBuildBuyResult BuildBuy,
    ManufacturingActivityResult Activity,
    ComponentProductionScheduleResult ComponentSchedule,
    ManufacturingCostResult Cost,
    ManufacturingTimelineResult Timeline,
    ManufacturingProfitabilityResult Profitability);