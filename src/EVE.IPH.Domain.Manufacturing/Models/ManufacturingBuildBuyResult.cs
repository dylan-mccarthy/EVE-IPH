namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingBuildBuyResult(
    bool CheaperToBuild,
    bool BuildItem);