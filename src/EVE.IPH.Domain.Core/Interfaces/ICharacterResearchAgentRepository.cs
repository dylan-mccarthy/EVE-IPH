using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Persists and retrieves a character's current research-agent state.
/// </summary>
public interface ICharacterResearchAgentRepository
{
    Task<Result<IReadOnlyList<CharacterResearchAgentRecord>>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CharacterResearchAgentRecord>>> ReplaceAsync(
        CharacterId characterId,
        IReadOnlyList<CharacterResearchAgentRecord> researchAgents,
        CancellationToken cancellationToken = default);
}

/// <summary>A stored current research-agent row.</summary>
public sealed record CharacterResearchAgentRecord(
    CharacterId CharacterId,
    long AgentId,
    TypeId SkillTypeId,
    double PointsPerDay,
    DateTimeOffset ResearchStartDate,
    double RemainderPoints);