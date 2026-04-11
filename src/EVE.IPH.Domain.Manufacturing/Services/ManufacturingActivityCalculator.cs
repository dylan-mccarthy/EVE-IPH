using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Models;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class ManufacturingActivityCalculator
{
    public Result<ManufacturingActivityResult> Calculate(ManufacturingActivityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.IncludeCopyUsage || input.IncludeInventionUsage)
        {
            if (input.TotalInventedRuns <= 0)
            {
                return Result<ManufacturingActivityResult>.Failure("INVALID_TOTAL_INVENTED_RUNS", "Total invented runs must be greater than zero when usage is included.");
            }
        }

        if (input.NumberOfInventionJobs < 0 || input.NumberOfInventionSessions < 0 || input.UserCopyRuns < 0)
        {
            return Result<ManufacturingActivityResult>.Failure("INVALID_ACTIVITY_COUNTS", "Activity counts must be zero or greater.");
        }

        if (input.CopyCostIndex < 0 || input.InventionCostIndex < 0)
        {
            return Result<ManufacturingActivityResult>.Failure("INVALID_COST_INDEX", "Cost indices must be zero or greater.");
        }

        if (input.CopyFacilityCostMultiplier <= 0 || input.InventionFacilityCostMultiplier <= 0)
        {
            return Result<ManufacturingActivityResult>.Failure("INVALID_COST_MULTIPLIER", "Facility cost multipliers must be greater than zero.");
        }

        if (input.CopyFacilityTimeMultiplier <= 0 || input.InventionFacilityTimeMultiplier <= 0)
        {
            return Result<ManufacturingActivityResult>.Failure("INVALID_TIME_MULTIPLIER", "Facility time multipliers must be greater than zero.");
        }

        double copyUsagePerRun = 0;
        if (input.IncludeCopyUsage && !input.IsTech3)
        {
            double copyJobGrossCost = (input.EstimatedItemValue * 0.02d)
                * input.CopyCostIndex
                * input.CopyFwCostBonus
                * input.CopyFacilityCostMultiplier;
            double copyFacilityTax = copyJobGrossCost * input.CopyFacilityTaxRate;
            copyUsagePerRun = ((copyJobGrossCost + copyFacilityTax) * input.NumberOfInventionJobs) / input.TotalInventedRuns;
        }

        double inventionUsagePerRun = 0;
        if (input.IncludeInventionUsage)
        {
            double inventionJobGrossCost = (input.EstimatedItemValue * 0.02d)
                * input.InventionCostIndex
                * input.InventionFwCostBonus
                * input.InventionFacilityCostMultiplier;
            double inventionFacilityTax = inventionJobGrossCost * input.InventionFacilityTaxRate;
            inventionUsagePerRun = ((inventionJobGrossCost + inventionFacilityTax) * input.NumberOfInventionJobs) / input.TotalInventedRuns;
        }

        double copyTimeSeconds = 0;
        if (input.IncludeCopyTime && !input.IsTech3)
        {
            double singleCopyTimeSeconds = input.BaseCopyTimeSeconds
                * (1 - (0.05d * input.ScienceSkillLevel))
                * (1 - (0.03d * input.AdvancedIndustrySkillLevel))
                * input.CopyFacilityTimeMultiplier
                * (1 - input.CopyImplantBonus);
            copyTimeSeconds = singleCopyTimeSeconds * input.UserCopyRuns;
        }

        double inventionTimeSeconds = 0;
        if (input.IncludeInventionTime)
        {
            double singleInventionTimeSeconds = input.IsTech3
                ? 3600d
                : input.BaseInventionTimeSeconds
                    * input.InventionFacilityTimeMultiplier
                    * (1 - (0.03d * input.AdvancedIndustrySkillLevel));

            inventionTimeSeconds = singleInventionTimeSeconds * input.NumberOfInventionSessions;
        }

        return Result<ManufacturingActivityResult>.Success(new ManufacturingActivityResult(
            copyUsagePerRun,
            inventionUsagePerRun,
            copyTimeSeconds,
            inventionTimeSeconds));
    }
}