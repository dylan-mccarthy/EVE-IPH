namespace EVE.IPH.Domain.Characters.Models;

/// <summary>
/// Represents one base NPC standing entry for a character.
/// </summary>
public sealed record NpcStanding(
    long NpcId,
    string NpcType,
    string NpcName,
    double Value);