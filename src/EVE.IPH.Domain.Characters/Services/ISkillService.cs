using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Characters.Services;

/// <summary>
/// Resolves character skill levels, including local override rules.
/// </summary>
public interface ISkillService
{
    Maybe<Skill> GetSkill(IReadOnlyList<Skill> skills, TypeId skillTypeId);

    int GetSkillLevel(IReadOnlyList<Skill> skills, TypeId skillTypeId, bool useActiveSkillLevels);
}