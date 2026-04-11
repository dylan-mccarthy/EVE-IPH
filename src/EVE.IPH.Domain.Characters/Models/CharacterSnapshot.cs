using EVE.IPH.Domain.Core.Interfaces;

namespace EVE.IPH.Domain.Characters.Models;

/// <summary>
/// Aggregated character state loaded into the modern domain layer.
/// </summary>
public sealed record CharacterSnapshot(
    CharacterRecord Character,
    IReadOnlyList<Skill> Skills,
    IReadOnlyList<NpcStanding> Standings);