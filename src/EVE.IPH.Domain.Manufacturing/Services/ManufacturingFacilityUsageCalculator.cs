using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Models;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class ManufacturingFacilityUsageCalculator
{
    public Result<ManufacturingFacilityUsageResult> Calculate(ManufacturingFacilityUsageInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.UserRuns < 0)
        {
            return Result<ManufacturingFacilityUsageResult>.Failure("INVALID_USER_RUNS", "User runs must be zero or greater.");
        }

        if (input.EstimatedItemValue < 0
            || input.CostIndex < 0
            || input.CostMultiplier <= 0
            || input.FwManufacturingCostBonus <= 0
            || input.FacilityTaxRate < 0
            || input.SccIndustryFeeSurcharge < 0
            || input.AlphaAccountTaxRate < 0)
        {
            return Result<ManufacturingFacilityUsageResult>.Failure("INVALID_FACILITY_USAGE_INPUT", "Facility usage inputs must be valid non-negative values, and multipliers must be greater than zero.");
        }

        if (!input.IncludeManufacturingUsage)
        {
            return Result<ManufacturingFacilityUsageResult>.Success(new ManufacturingFacilityUsageResult(0, 0, 0, 0, 0));
        }

        double totalEstimatedItemValue = Math.Round(input.EstimatedItemValue * input.UserRuns, MidpointRounding.ToEven);
        double indexBonuses = input.CostIndex * input.CostMultiplier * input.FwManufacturingCostBonus;
        double alphaCloneTax = input.IsAlphaAccount ? input.AlphaAccountTaxRate : 0;
        double modifiedSccSurcharge = input.HasFulcrumPirateReduction
            ? input.SccIndustryFeeSurcharge * 0.1d
            : input.SccIndustryFeeSurcharge;

        double facilityUsage = totalEstimatedItemValue * (indexBonuses + input.FacilityTaxRate + modifiedSccSurcharge + alphaCloneTax);

        if (input.IsReactionManufacturing)
        {
            return Result<ManufacturingFacilityUsageResult>.Success(new ManufacturingFacilityUsageResult(
                facilityUsage,
                facilityUsage,
                0,
                facilityUsage,
                facilityUsage));
        }

        return Result<ManufacturingFacilityUsageResult>.Success(new ManufacturingFacilityUsageResult(
            facilityUsage,
            facilityUsage,
            facilityUsage,
            0,
            0));
    }
}