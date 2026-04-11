namespace EVE.IPH.Domain.Reprocessing.Models;

public sealed record OreConversionInput(
    IReadOnlyList<OreConversionRequirement> Requirements,
    IReadOnlyList<OreConversionCandidate> Candidates);