using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Characters.Services;

/// <summary>
/// Applies the legacy skill-selection rules for trained, active, and overridden levels.
/// </summary>
public sealed class SkillService : ISkillService
{
    public Maybe<Skill> GetSkill(IReadOnlyList<Skill> skills, TypeId skillTypeId)
    {
        ArgumentNullException.ThrowIfNull(skills);

        foreach (Skill skill in skills)
        {
            if (skill.SkillTypeId == skillTypeId)
            {
                return Maybe<Skill>.Some(skill);
            }
        }

        return Maybe<Skill>.None;
    }

    public int GetSkillLevel(IReadOnlyList<Skill> skills, TypeId skillTypeId, bool useActiveSkillLevels)
    {
        Maybe<Skill> skill = GetSkill(skills, skillTypeId);
        return skill.HasValue ? skill.Value.GetEffectiveLevel(useActiveSkillLevels) : 0;
    }
}