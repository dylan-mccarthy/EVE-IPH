using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Reads static research-agent metadata from the application database.
/// </summary>
public interface IResearchAgentDefinitionRepository
{
    Task<Result<IReadOnlyDictionary<long, ResearchAgentDefinitionRecord>>> GetByAgentIdsAsync(
        IReadOnlyCollection<long> agentIds,
        CancellationToken cancellationToken = default);
}

/// <summary>Static metadata for a research agent.</summary>
public sealed record ResearchAgentDefinitionRecord(
    long AgentId,
    string AgentName,
    double ResearchPointsPerDay,
    int Level,
    string Location);