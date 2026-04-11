using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Models;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class ManufacturingJobCalculator
{
    public Result<ManufacturingJobResult> Calculate(ManufacturingJobInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Runs <= 0)
        {
            return Result<ManufacturingJobResult>.Failure("INVALID_RUNS", "Runs must be greater than zero.");
        }

        if (input.BaseMaterialQuantity <= 0)
        {
            return Result<ManufacturingJobResult>.Failure("INVALID_BASE_MATERIAL_QUANTITY", "Base material quantity must be greater than zero.");
        }

        if (input.BaseProductionTimeSeconds <= 0)
        {
            return Result<ManufacturingJobResult>.Failure("INVALID_BASE_PRODUCTION_TIME", "Base production time must be greater than zero.");
        }

        if (input.AvailableBlueprints <= 0 || input.AvailableProductionLines <= 0)
        {
            return Result<ManufacturingJobResult>.Failure("INVALID_JOB_CAPACITY", "Available blueprints and production lines must both be greater than zero.");
        }

        if (input.FacilityMaterialMultiplier <= 0 || input.FacilityTimeMultiplier <= 0)
        {
            return Result<ManufacturingJobResult>.Failure("INVALID_FACILITY_MULTIPLIER", "Facility multipliers must be greater than zero.");
        }

        if (input.ImplantTimeMultiplier <= 0 || input.SpecializedTimeMultiplier <= 0)
        {
            return Result<ManufacturingJobResult>.Failure("INVALID_TIME_MULTIPLIER", "Time multipliers must be greater than zero.");
        }

        double facilityMaterialMultiplier = input.HasFulcrumMaterialBonus ? 0.94 : input.FacilityMaterialMultiplier;
        double materialModifier = (1 - (input.BlueprintMaterialEfficiencyPercent / 100d)) * facilityMaterialMultiplier;

        long requiredMaterialQuantity = (long)Math.Max(
            input.Runs,
            Math.Ceiling(Math.Round(input.Runs * input.BaseMaterialQuantity * materialModifier, 2)));

        long jobsPerBatch = Math.Min(input.AvailableBlueprints, input.AvailableProductionLines);
        if (jobsPerBatch > input.Runs)
        {
            jobsPerBatch = input.Runs;
        }

        double singleRunDurationSeconds = input.BaseProductionTimeSeconds
            * (1 - (input.BlueprintTimeEfficiencyPercent / 100d))
            * input.FacilityTimeMultiplier
            * input.ImplantTimeMultiplier
            * (1 - (input.IndustrySkillLevel * 0.04d))
            * (1 - (input.AdvancedIndustrySkillLevel * 0.03d))
            * input.SpecializedTimeMultiplier;

        long fullJobSessions = (long)Math.Ceiling(input.Runs / (double)jobsPerBatch);
        double totalJobDurationSeconds = fullJobSessions * singleRunDurationSeconds;

        return Result<ManufacturingJobResult>.Success(new ManufacturingJobResult(
            requiredMaterialQuantity,
            singleRunDurationSeconds,
            totalJobDurationSeconds,
            jobsPerBatch,
            fullJobSessions));
    }
}