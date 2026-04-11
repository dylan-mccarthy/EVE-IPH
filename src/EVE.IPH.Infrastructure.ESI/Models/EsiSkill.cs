using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Infrastructure.ESI;

/// <summary>
/// One skill entry returned by the character skills endpoint.
/// </summary>
public sealed record EsiSkill(
    TypeId SkillTypeId,
    int ActiveSkillLevel,
    int TrainedSkillLevel,
    long SkillPointsInSkill);