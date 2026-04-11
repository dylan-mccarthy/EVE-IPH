namespace EVE.IPH.Domain.Reprocessing.Models;

public sealed record ReprocessingCalculationInput(
    int UnitsPerBatch,
    double TotalQuantity,
    int BaseMaterialQuantityPerBatch,
    double FacilityMaterialMultiplier,
    int ReprocessingSkillLevel,
    int ReprocessingEfficiencySkillLevel,
    int ProcessingSkillLevel,
    double ImplantBonus,
    bool IsScrapReprocessing = false,
    double ScrapBaseYield = 0d);