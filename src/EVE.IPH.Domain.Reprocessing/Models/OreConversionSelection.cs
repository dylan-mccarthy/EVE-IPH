namespace EVE.IPH.Domain.Reprocessing.Models;

public sealed record OreConversionSelection(
    string OreName,
    string GroupName,
    long BatchCount,
    long TotalOreQuantity,
    double ObjectiveValue,
    double ReprocessingUsage);