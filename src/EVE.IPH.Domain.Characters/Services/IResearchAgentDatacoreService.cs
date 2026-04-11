using EVE.IPH.Domain.Characters.Models;

namespace EVE.IPH.Domain.Characters.Services;

public interface IResearchAgentDatacoreService
{
    ResearchAgentDatacoreSummary BuildSummary(
        IEnumerable<ResearchAgent> agents,
        IReadOnlyDictionary<string, double> datacorePrices,
        double datacoreRedeemCost);
}