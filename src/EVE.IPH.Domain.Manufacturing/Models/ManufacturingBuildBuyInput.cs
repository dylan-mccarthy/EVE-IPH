namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingBuildBuyInput(
    string BlueprintName,
    double OneItemMarketCost,
    long PortionSize,
    long Runs,
    double TotalBuildCost,
    double NetExcessSaleAmount,
    bool OwnedBlueprint,
    bool IsNewBlueprintRequest,
    bool SuggestBuildWhenBlueprintNotOwned,
    bool AlwaysBuyFuelBlocks,
    bool AlwaysBuyRams,
    bool ForceBuildBecauseMarketInsufficient,
    bool? ManualBuildOverride);