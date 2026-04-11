namespace EVE.IPH.Domain.Reprocessing.Models;

public sealed record OreConversionRequirement(
    string MaterialName,
    double RequiredQuantity);