namespace EVE.IPH.Domain.Reprocessing.Models;

public sealed record OreConversionResult(
    IReadOnlyList<OreConversionSelection> Selections,
    IReadOnlyList<OreConversionExcessMaterial> ExcessMaterials,
    double TotalObjectiveValue,
    double TotalReprocessingUsage);