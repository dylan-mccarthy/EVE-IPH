using EVE.IPH.Domain.Characters.Models;

namespace EVE.IPH.Domain.Characters.Services;

/// <summary>
/// Resolves base and effective standings using the legacy Connections and Diplomacy rules.
/// </summary>
public interface IStandingsService
{
    double GetStanding(IReadOnlyList<NpcStanding> standings, long npcId);

    double GetStanding(IReadOnlyList<NpcStanding> standings, string npcName);

    double GetEffectiveStanding(double baseStanding, int connectionsLevel, int diplomacyLevel);

    double GetEffectiveStanding(IReadOnlyList<NpcStanding> standings, long npcId, int connectionsLevel, int diplomacyLevel);

    double GetEffectiveStanding(IReadOnlyList<NpcStanding> standings, string npcName, int connectionsLevel, int diplomacyLevel);
}