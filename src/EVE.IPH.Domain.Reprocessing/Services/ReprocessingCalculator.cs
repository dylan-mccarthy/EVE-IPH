using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Reprocessing.Models;

namespace EVE.IPH.Domain.Reprocessing.Services;

public sealed class ReprocessingCalculator
{
    public Result<ReprocessingCalculationResult> Calculate(ReprocessingCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.UnitsPerBatch <= 0)
        {
            return Result<ReprocessingCalculationResult>.Failure("INVALID_UNITS_PER_BATCH", "Units per batch must be greater than zero.");
        }

        if (input.TotalQuantity <= 0)
        {
            return Result<ReprocessingCalculationResult>.Failure("INVALID_TOTAL_QUANTITY", "Total quantity must be greater than zero.");
        }

        if (input.BaseMaterialQuantityPerBatch < 0)
        {
            return Result<ReprocessingCalculationResult>.Failure("INVALID_BASE_MATERIAL_QUANTITY", "Base material quantity per batch cannot be negative.");
        }

        if (input.IsScrapReprocessing)
        {
            if (input.ScrapBaseYield <= 0)
            {
                return Result<ReprocessingCalculationResult>.Failure("INVALID_SCRAP_BASE_YIELD", "Scrap base yield must be greater than zero.");
            }
        }
        else if (input.FacilityMaterialMultiplier <= 0)
        {
            return Result<ReprocessingCalculationResult>.Failure("INVALID_FACILITY_MULTIPLIER", "Facility material multiplier must be greater than zero.");
        }

        long refineBatches = (long)Math.Floor(input.TotalQuantity / input.UnitsPerBatch);
        if (refineBatches <= 0)
        {
            return Result<ReprocessingCalculationResult>.Success(new ReprocessingCalculationResult(0, 0d, 0));
        }

        double totalYield = input.IsScrapReprocessing
            ? input.ScrapBaseYield * (1 + (0.02d * input.ProcessingSkillLevel))
            : input.FacilityMaterialMultiplier
                * (1 + (0.03d * input.ReprocessingSkillLevel))
                * (1 + (0.02d * input.ReprocessingEfficiencySkillLevel))
                * (1 + (0.02d * input.ProcessingSkillLevel))
                * (1 + input.ImplantBonus);

        if (totalYield > 1d)
        {
            totalYield = 1d;
        }

        long recoveredMaterialQuantity = (long)Math.Floor(input.BaseMaterialQuantityPerBatch * refineBatches * totalYield);

        return Result<ReprocessingCalculationResult>.Success(new ReprocessingCalculationResult(
            refineBatches,
            totalYield,
            recoveredMaterialQuantity));
    }
}