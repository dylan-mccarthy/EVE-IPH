using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Fetches a character's current research-agent progress from an external source.
/// </summary>
public interface ICharacterResearchAgentDataSource
{
    Task<Result<IReadOnlyList<CharacterResearchAgentData>>> GetResearchAgentsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}

/// <summary>Current research-agent data returned by an external source.</summary>
public sealed record CharacterResearchAgentData(
    long AgentId,
    TypeId SkillTypeId,
    DateTimeOffset ResearchStartDate,
    double PointsPerDay,
    double RemainderPoints);