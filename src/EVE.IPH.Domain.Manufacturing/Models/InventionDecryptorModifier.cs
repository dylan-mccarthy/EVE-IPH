namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record InventionDecryptorModifier(
    int RunModifier,
    double ProbabilityModifier = 1.0);