namespace EVE.IPH.Domain.Reprocessing.Models;

public sealed record OreConversionCandidate(
    string OreName,
    string GroupName,
    int UnitsPerBatch,
    double ObjectiveValuePerBatch,
    double ReprocessingUsagePerBatch,
    IReadOnlyList<OreConversionYield> Yields);