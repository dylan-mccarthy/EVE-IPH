namespace EVE.IPH.Domain.Reprocessing.Models;

public sealed record OreConversionYield(
    string MaterialName,
    double QuantityPerBatch);