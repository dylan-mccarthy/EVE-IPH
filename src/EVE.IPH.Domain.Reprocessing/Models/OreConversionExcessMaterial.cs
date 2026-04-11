namespace EVE.IPH.Domain.Reprocessing.Models;

public sealed record OreConversionExcessMaterial(
    string MaterialName,
    double RequiredQuantity,
    double ProducedQuantity,
    double ExcessQuantity);