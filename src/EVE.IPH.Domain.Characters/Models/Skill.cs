using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Characters.Models;

/// <summary>
/// Represents one character skill together with any local override metadata.
/// </summary>
public sealed record Skill(
    TypeId SkillTypeId,
    string Name,
    int TrainedLevel,
    int ActiveLevel,
    long SkillPoints,
    bool IsOverridden,
    int OverrideLevel)
{
    public int GetEffectiveLevel(bool useActiveSkillLevels) =>
        IsOverridden ? OverrideLevel : useActiveSkillLevels ? ActiveLevel : TrainedLevel;
}