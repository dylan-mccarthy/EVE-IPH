using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Models;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class ManufacturingUsageAllocationCalculator
{
    public Result<ManufacturingUsageAllocationResult> Calculate(ManufacturingUsageAllocationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.FacilityUsage);

        if (input.ComponentFacilityUsage < 0
            || input.CapitalComponentFacilityUsage < 0
            || input.TotalReactionFacilityUsage < 0
            || input.ReprocessingUsage < 0)
        {
            return Result<ManufacturingUsageAllocationResult>.Failure("INVALID_USAGE_VALUE", "Usage values must be zero or greater.");
        }

        double componentUsage = 0;
        double mainFacilityUsage;
        double remainingReactionUsage = 0;

        if (input.IncludeComponentManufacturingUsage)
        {
            componentUsage += input.ComponentFacilityUsage;
        }

        if (input.IncludeCapitalComponentManufacturingUsage)
        {
            componentUsage += input.CapitalComponentFacilityUsage;
        }

        if (input.IncludeReactionUsage && input.FacilityUsage.ReactionFacilityUsage > 0)
        {
            mainFacilityUsage = input.FacilityUsage.ReactionFacilityUsage;
            componentUsage += input.FacilityUsage.ManufacturingFacilityUsage;
            remainingReactionUsage = input.TotalReactionFacilityUsage - input.FacilityUsage.ReactionFacilityUsage;
        }
        else
        {
            mainFacilityUsage = input.FacilityUsage.ManufacturingFacilityUsage;
            componentUsage += input.FacilityUsage.ReactionFacilityUsage;
        }

        double reprocessingUsage = input.HasReprocessingFacility && !input.IncludeReprocessingUsage
            ? 0
            : input.ReprocessingUsage;

        double totalUsage = mainFacilityUsage + componentUsage + remainingReactionUsage + reprocessingUsage;

        return Result<ManufacturingUsageAllocationResult>.Success(new ManufacturingUsageAllocationResult(
            mainFacilityUsage,
            componentUsage,
            remainingReactionUsage,
            reprocessingUsage,
            totalUsage));
    }
}