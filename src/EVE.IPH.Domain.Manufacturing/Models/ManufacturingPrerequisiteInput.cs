namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingPrerequisiteInput(
    IReadOnlyList<ManufacturingSkillRequirement> RequiredSkills,
    IReadOnlyDictionary<long, int> CharacterSkillLevels);