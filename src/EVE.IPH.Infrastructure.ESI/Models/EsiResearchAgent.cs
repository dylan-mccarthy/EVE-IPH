using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Infrastructure.ESI;

/// <summary>
/// One active research-agent entry returned by the character research-agent endpoint.
/// </summary>
public sealed record EsiResearchAgent(
    long AgentId,
    TypeId SkillTypeId,
    DateTimeOffset StartedAt,
    double PointsPerDay,
    double RemainderPoints);