using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Models;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class ManufacturingCostCalculator
{
    public Result<ManufacturingCostResult> Calculate(ManufacturingCostInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.UserRuns < 0)
        {
            return Result<ManufacturingCostResult>.Failure("INVALID_USER_RUNS", "User runs must be zero or greater.");
        }

        if (input.IncludeCopyCosts && !input.IsTech3 && input.TotalInventedRuns <= 0)
        {
            return Result<ManufacturingCostResult>.Failure("INVALID_TOTAL_INVENTED_RUNS", "Total invented runs must be greater than zero when copy costs are included.");
        }

        double inventionCost = input.IncludeInventionCosts
            ? input.PerInventionRunCost * input.UserRuns
            : 0;

        double copyCost = input.IncludeCopyCosts && !input.IsTech3
            ? (input.TotalCopyCost / input.TotalInventedRuns) * input.UserRuns
            : 0;

        double totalUsage = input.MainFacilityUsage
            + input.ComponentUsage
            + input.InventionUsage
            + input.CopyUsage
            + input.RemainingReactionUsage
            + input.ReprocessingUsage;

        return Result<ManufacturingCostResult>.Success(new ManufacturingCostResult(
            inventionCost,
            copyCost,
            totalUsage));
    }
}