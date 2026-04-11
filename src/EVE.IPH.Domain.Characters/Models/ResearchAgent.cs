using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Characters.Models;

/// <summary>
/// A character's active research agent with computed current research points.
/// </summary>
public sealed record ResearchAgent(
    long AgentId,
    TypeId SkillTypeId,
    string AgentName,
    string Field,
    double CurrentResearchPoints,
    double PointsPerDay,
    int AgentLevel,
    string Location,
    DateTimeOffset ResearchStartDate,
    double RemainderPoints);