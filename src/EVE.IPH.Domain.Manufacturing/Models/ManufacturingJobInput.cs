namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingJobInput(
    long Runs,
    long BaseMaterialQuantity,
    int BlueprintMaterialEfficiencyPercent,
    int BlueprintTimeEfficiencyPercent,
    long BaseProductionTimeSeconds,
    int AvailableBlueprints,
    int AvailableProductionLines,
    int IndustrySkillLevel,
    int AdvancedIndustrySkillLevel,
    double FacilityMaterialMultiplier,
    double FacilityTimeMultiplier,
    double ImplantTimeMultiplier = 1.0,
    double SpecializedTimeMultiplier = 1.0,
    bool HasFulcrumMaterialBonus = false);