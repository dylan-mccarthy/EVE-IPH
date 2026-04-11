using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Models;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class ManufacturingBuildBuyDecider
{
    public Result<ManufacturingBuildBuyResult> Calculate(ManufacturingBuildBuyInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.PortionSize <= 0 || input.Runs <= 0)
        {
            return Result<ManufacturingBuildBuyResult>.Failure("INVALID_BUILD_BUY_QUANTITY", "Portion size and runs must be greater than zero.");
        }

        if (input.OneItemMarketCost < 0 || input.TotalBuildCost < 0 || input.NetExcessSaleAmount < 0)
        {
            return Result<ManufacturingBuildBuyResult>.Failure("INVALID_BUILD_BUY_COST", "Build-buy costs must be zero or greater.");
        }

        if ((input.AlwaysBuyFuelBlocks && input.BlueprintName.Contains("Fuel Block", StringComparison.OrdinalIgnoreCase))
            || (input.AlwaysBuyRams && input.BlueprintName.Contains("R.A.M.", StringComparison.OrdinalIgnoreCase)))
        {
            return Result<ManufacturingBuildBuyResult>.Success(new ManufacturingBuildBuyResult(false, false));
        }

        bool cheaperToBuild = ((input.OneItemMarketCost * input.PortionSize) * input.Runs) > (input.TotalBuildCost - input.NetExcessSaleAmount);
        bool buildItem = cheaperToBuild;

        if (input.IsNewBlueprintRequest)
        {
            buildItem = (input.OneItemMarketCost == 0)
                || (cheaperToBuild && (input.SuggestBuildWhenBlueprintNotOwned || (input.OwnedBlueprint && !input.SuggestBuildWhenBlueprintNotOwned)));

            if (input.ForceBuildBecauseMarketInsufficient)
            {
                buildItem = true;
            }
        }
        else
        {
            if (input.ForceBuildBecauseMarketInsufficient)
            {
                buildItem = true;
            }

            if (input.ManualBuildOverride.HasValue)
            {
                buildItem = input.ManualBuildOverride.Value;
            }
        }

        return Result<ManufacturingBuildBuyResult>.Success(new ManufacturingBuildBuyResult(cheaperToBuild, buildItem));
    }
}