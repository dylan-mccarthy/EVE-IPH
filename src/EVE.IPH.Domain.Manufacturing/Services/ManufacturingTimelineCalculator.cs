using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Models;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class ManufacturingTimelineCalculator
{
    public Result<ManufacturingTimelineResult> Calculate(ManufacturingTimelineInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.BaseBlueprintProductionTimeSeconds < 0
            || input.CopyTimeSeconds < 0
            || input.InventionTimeSeconds < 0
            || input.ComponentProductionTimeSeconds < 0)
        {
            return Result<ManufacturingTimelineResult>.Failure("INVALID_PRODUCTION_TIME", "Production time values must be zero or greater.");
        }

        double blueprintProductionTimeSeconds = input.BaseBlueprintProductionTimeSeconds
            + input.CopyTimeSeconds
            + input.InventionTimeSeconds;

        double totalProductionTimeSeconds = input.ComponentProductionTimeSeconds
            + blueprintProductionTimeSeconds;

        return Result<ManufacturingTimelineResult>.Success(new ManufacturingTimelineResult(
            blueprintProductionTimeSeconds,
            totalProductionTimeSeconds));
    }
}