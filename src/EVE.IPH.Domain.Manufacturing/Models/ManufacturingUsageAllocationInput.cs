namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingUsageAllocationInput(
    ManufacturingFacilityUsageResult FacilityUsage,
    bool IncludeComponentManufacturingUsage,
    double ComponentFacilityUsage,
    bool IncludeCapitalComponentManufacturingUsage,
    double CapitalComponentFacilityUsage,
    bool IncludeReactionUsage,
    double TotalReactionFacilityUsage,
    bool HasReprocessingFacility,
    bool IncludeReprocessingUsage,
    double ReprocessingUsage);