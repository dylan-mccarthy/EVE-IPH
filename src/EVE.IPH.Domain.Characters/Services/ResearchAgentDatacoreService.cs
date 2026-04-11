using EVE.IPH.Domain.Characters.Models;

namespace EVE.IPH.Domain.Characters.Services;

public sealed class ResearchAgentDatacoreService : IResearchAgentDatacoreService
{
    public ResearchAgentDatacoreSummary BuildSummary(
        IEnumerable<ResearchAgent> agents,
        IReadOnlyDictionary<string, double> datacorePrices,
        double datacoreRedeemCost)
    {
        ArgumentNullException.ThrowIfNull(agents);
        ArgumentNullException.ThrowIfNull(datacorePrices);

        List<ResearchAgentDatacoreSnapshot> snapshots = [];

        foreach (ResearchAgent agent in agents)
        {
            ArgumentNullException.ThrowIfNull(agent);

            string datacoreName = GetDatacoreName(agent.Field);
            long currentDatacores = (long)Math.Floor(agent.CurrentResearchPoints / 100d);
            double price = datacorePrices.GetValueOrDefault(datacoreName, 0d);
            double currentValue = currentDatacores * (price - datacoreRedeemCost);

            snapshots.Add(new ResearchAgentDatacoreSnapshot(
                agent.AgentName,
                agent.Field,
                datacoreName,
                agent.CurrentResearchPoints,
                currentDatacores,
                currentValue,
                agent.PointsPerDay,
                agent.AgentLevel,
                agent.Location));
        }

        return new ResearchAgentDatacoreSummary(snapshots, snapshots.Sum(agent => agent.CurrentValue));
    }

    private static string GetDatacoreName(string field)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        if (field.Contains("Gallente Starship", StringComparison.OrdinalIgnoreCase))
        {
            return "Datacore - Gallentean Starship Engineering";
        }

        if (field.Contains("Amarr Starship", StringComparison.OrdinalIgnoreCase))
        {
            return "Datacore - Amarian Starship Engineering";
        }

        return "Datacore - " + field;
    }
}