using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Models;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class InventionCalculator
{
    public Result<InventionPlanResult> Calculate(InventionPlanInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Decryptor);
        ArgumentNullException.ThrowIfNull(input.SupportingSkillLevels);

        if (input.UserRuns <= 0)
        {
            return Result<InventionPlanResult>.Failure("INVALID_USER_RUNS", "User runs must be greater than zero.");
        }

        if (input.MaxProductionLimit <= 0)
        {
            return Result<InventionPlanResult>.Failure("INVALID_MAX_PRODUCTION_LIMIT", "Max production limit must be greater than zero.");
        }

        if (input.NumberOfLaboratoryLines <= 0)
        {
            return Result<InventionPlanResult>.Failure("INVALID_LABORATORY_LINES", "Laboratory lines must be greater than zero.");
        }

        if (input.BaseInventionChance <= 0)
        {
            return Result<InventionPlanResult>.Failure("INVALID_BASE_INVENTION_CHANCE", "Base invention chance must be greater than zero.");
        }

        if (input.Decryptor.ProbabilityModifier <= 0)
        {
            return Result<InventionPlanResult>.Failure("INVALID_DECRYPTOR_PROBABILITY", "Decryptor probability modifier must be greater than zero.");
        }

        int singleInventedBlueprintRuns = input.MaxProductionLimit + input.Decryptor.RunModifier;
        if (singleInventedBlueprintRuns <= 0)
        {
            return Result<InventionPlanResult>.Failure("INVALID_INVENTED_RUNS", "Single invented blueprint runs must be greater than zero.");
        }

        double inventionChance;
        if (input.UseTypicalSkills)
        {
            inventionChance = input.BaseInventionChance * (1 + (((4d + 4d) / 30d) + (4d / 40d))) * input.Decryptor.ProbabilityModifier;
        }
        else
        {
            int totalSupportingSkillLevels = input.SupportingSkillLevels.Sum();
            inventionChance = input.BaseInventionChance
                * (1 + (totalSupportingSkillLevels / 30d) + (input.EncryptionSkillLevel / 40d))
                * input.Decryptor.ProbabilityModifier;
        }

        if (inventionChance <= 0)
        {
            return Result<InventionPlanResult>.Failure("INVALID_INVENTION_CHANCE", "Calculated invention chance must be greater than zero.");
        }

        int requiredBlueprintCopies = (int)Math.Ceiling(input.UserRuns / (double)singleInventedBlueprintRuns);
        int numberOfInventionJobs = (int)Math.Ceiling((1 / inventionChance) * requiredBlueprintCopies);
        int totalInventedRuns = requiredBlueprintCopies * singleInventedBlueprintRuns;
        int numberOfInventionSessions = (int)Math.Ceiling(numberOfInventionJobs / (double)input.NumberOfLaboratoryLines);
        double perInventionRunCost = (input.SingleInventionMaterialsCost / inventionChance) / singleInventedBlueprintRuns;

        return Result<InventionPlanResult>.Success(new InventionPlanResult(
            inventionChance,
            singleInventedBlueprintRuns,
            requiredBlueprintCopies,
            numberOfInventionJobs,
            numberOfInventionSessions,
            totalInventedRuns,
            perInventionRunCost));
    }
}