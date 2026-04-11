using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Core;

/// <summary>
/// Reserved synthetic characters used by the modern host for local-only workflows.
/// </summary>
public static class SpecialCharacters
{
    public static CharacterId AllSkillsVId { get; } = new(-1);

    public static CorporationId PlaceholderCorporationId { get; } = new(0);

    public const string AllSkillsVName = "All Skills V";

    public static bool IsAllSkillsV(CharacterId characterId) => characterId == AllSkillsVId;
}