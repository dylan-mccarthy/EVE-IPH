using EVE.IPH.Domain.Characters.Models;

namespace EVE.IPH.Domain.Characters.Services;

/// <summary>
/// Implements standing lookup and effective-standing calculation from the legacy client.
/// </summary>
public sealed class StandingsService : IStandingsService
{
    public double GetStanding(IReadOnlyList<NpcStanding> standings, long npcId)
    {
        ArgumentNullException.ThrowIfNull(standings);

        foreach (NpcStanding standing in standings)
        {
            if (standing.NpcId == npcId)
            {
                return standing.Value;
            }
        }

        return 0;
    }

    public double GetStanding(IReadOnlyList<NpcStanding> standings, string npcName)
    {
        ArgumentNullException.ThrowIfNull(standings);
        ArgumentException.ThrowIfNullOrWhiteSpace(npcName);

        foreach (NpcStanding standing in standings)
        {
            if (string.Equals(standing.NpcName, npcName, StringComparison.Ordinal))
            {
                return standing.Value;
            }
        }

        return 0;
    }

    public double GetEffectiveStanding(double baseStanding, int connectionsLevel, int diplomacyLevel)
    {
        if (baseStanding < 0)
        {
            return baseStanding + ((10 - baseStanding) * (0.04 * diplomacyLevel));
        }

        if (baseStanding > 0)
        {
            return baseStanding + ((10 - baseStanding) * (0.04 * connectionsLevel));
        }

        return 0;
    }

    public double GetEffectiveStanding(IReadOnlyList<NpcStanding> standings, long npcId, int connectionsLevel, int diplomacyLevel) =>
        GetEffectiveStanding(GetStanding(standings, npcId), connectionsLevel, diplomacyLevel);

    public double GetEffectiveStanding(IReadOnlyList<NpcStanding> standings, string npcName, int connectionsLevel, int diplomacyLevel) =>
        GetEffectiveStanding(GetStanding(standings, npcName), connectionsLevel, diplomacyLevel);
}