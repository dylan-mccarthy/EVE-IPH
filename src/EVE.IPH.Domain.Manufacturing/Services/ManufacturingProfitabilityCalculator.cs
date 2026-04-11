using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Models;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class ManufacturingProfitabilityCalculator
{
    public Result<ManufacturingProfitabilityResult> Calculate(ManufacturingProfitabilityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.TotalProductionTimeSeconds <= 0)
        {
            return Result<ManufacturingProfitabilityResult>.Failure("INVALID_TOTAL_PRODUCTION_TIME", "Total production time must be greater than zero.");
        }

        if (input.BlueprintProductionTimeSeconds <= 0)
        {
            return Result<ManufacturingProfitabilityResult>.Failure("INVALID_BLUEPRINT_PRODUCTION_TIME", "Blueprint production time must be greater than zero.");
        }

        double totalRawCost = input.RawMaterialsCost
            + input.InventionCost
            + input.CopyCost
            + input.TaxesAndFees
            + input.AdditionalCosts
            + input.TotalUsage
            - input.SellExcessAmount;

        double totalComponentCost = input.ComponentMaterialsCost
            + input.InventionCost
            + input.CopyCost
            + input.TaxesAndFees
            + input.AdditionalCosts
            + (input.TotalUsage - input.ComponentUsage - input.RemainingReactionUsage - input.ReprocessingUsage)
            - input.SellExcessAmount;

        if (input.IsBuildBuy)
        {
            totalComponentCost += input.ComponentUsage + input.RemainingReactionUsage + input.ReprocessingUsage;
        }
        else
        {
            totalComponentCost += input.SellExcessAmount;
        }

        double totalRawProfit = input.ItemMarketCost - totalRawCost;
        double totalComponentProfit = input.ItemMarketCost - totalComponentCost;

        double totalRawProfitPercent;
        double totalComponentProfitPercent;

        if (input.ItemMarketCost == 0)
        {
            totalRawProfitPercent = 0;
            totalComponentProfitPercent = 0;
        }
        else
        {
            totalRawProfitPercent = 1 - (totalRawCost / input.ItemMarketCost);
            totalComponentProfitPercent = 1 - (totalComponentCost / input.ItemMarketCost);
        }

        double totalIskPerHourRaw = totalRawProfit / input.TotalProductionTimeSeconds * 3600;
        double totalIskPerHourComponent = input.IsBuildBuy
            ? totalIskPerHourRaw
            : totalComponentProfit / input.BlueprintProductionTimeSeconds * 3600;

        return Result<ManufacturingProfitabilityResult>.Success(new ManufacturingProfitabilityResult(
            totalRawCost,
            totalComponentCost,
            totalRawProfit,
            totalComponentProfit,
            totalRawProfitPercent,
            totalComponentProfitPercent,
            totalIskPerHourRaw,
            totalIskPerHourComponent));
    }
}