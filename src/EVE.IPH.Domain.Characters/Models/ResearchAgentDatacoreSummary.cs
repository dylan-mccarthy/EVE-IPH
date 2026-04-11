namespace EVE.IPH.Domain.Characters.Models;

public sealed record ResearchAgentDatacoreSummary(
    IReadOnlyList<ResearchAgentDatacoreSnapshot> Agents,
    double TotalValue);