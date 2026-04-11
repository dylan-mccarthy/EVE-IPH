namespace EVE.IPH.Domain.Characters.Models;

public sealed record ResearchAgentDatacoreSnapshot(
    string AgentName,
    string Field,
    string DatacoreName,
    double CurrentResearchPoints,
    long CurrentDatacores,
    double CurrentValue,
    double PointsPerDay,
    int AgentLevel,
    string Location);