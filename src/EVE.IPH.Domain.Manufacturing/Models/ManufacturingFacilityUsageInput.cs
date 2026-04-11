namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingFacilityUsageInput(
    bool IncludeManufacturingUsage,
    double EstimatedItemValue,
    int UserRuns,
    double CostIndex,
    double CostMultiplier,
    double FwManufacturingCostBonus,
    double FacilityTaxRate,
    double SccIndustryFeeSurcharge,
    bool IsAlphaAccount,
    double AlphaAccountTaxRate,
    bool IsReactionManufacturing,
    bool HasFulcrumPirateReduction);