using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Models;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class ManufacturingPrerequisiteService
{
    private static readonly HashSet<long> OnePercentReductionSkillIds =
    [
        3398, 3397, 3395, 11444, 11454, 11448, 11453, 11450, 11446, 11433,
        11443, 11447, 11452, 11445, 11529, 11451, 11441, 11455, 11449, 81050, 3400,
    ];

    private const long TwoPercentReductionSkillId = 81896;

    public Result<ManufacturingPrerequisiteResult> Calculate(ManufacturingPrerequisiteInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.RequiredSkills);
        ArgumentNullException.ThrowIfNull(input.CharacterSkillLevels);

        foreach (ManufacturingSkillRequirement requiredSkill in input.RequiredSkills)
        {
            if (requiredSkill.RequiredLevel <= 0)
            {
                return Result<ManufacturingPrerequisiteResult>.Failure("INVALID_REQUIRED_SKILL_LEVEL", "Required skill levels must be greater than zero.");
            }

            if (!input.CharacterSkillLevels.TryGetValue(requiredSkill.TypeId, out int characterLevel)
                || characterLevel < requiredSkill.RequiredLevel)
            {
                return Result<ManufacturingPrerequisiteResult>.Success(new ManufacturingPrerequisiteResult(false, CalculateAdvancedManufacturingTimeMultiplier(input.RequiredSkills, input.CharacterSkillLevels)));
            }
        }

        return Result<ManufacturingPrerequisiteResult>.Success(new ManufacturingPrerequisiteResult(
            true,
            CalculateAdvancedManufacturingTimeMultiplier(input.RequiredSkills, input.CharacterSkillLevels)));
    }

    private static double CalculateAdvancedManufacturingTimeMultiplier(
        IReadOnlyList<ManufacturingSkillRequirement> requiredSkills,
        IReadOnlyDictionary<long, int> characterSkillLevels)
    {
        double bonusSum = 1;

        foreach (ManufacturingSkillRequirement requiredSkill in requiredSkills)
        {
            characterSkillLevels.TryGetValue(requiredSkill.TypeId, out int characterLevel);

            if (OnePercentReductionSkillIds.Contains(requiredSkill.TypeId))
            {
                bonusSum *= 1 - (0.01d * characterLevel);
            }
            else if (requiredSkill.TypeId == TwoPercentReductionSkillId)
            {
                bonusSum *= 1 - (0.02d * characterLevel);
            }
        }

        return bonusSum;
    }
}