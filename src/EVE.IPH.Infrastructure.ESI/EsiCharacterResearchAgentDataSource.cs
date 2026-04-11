using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Interfaces;

namespace EVE.IPH.Infrastructure.ESI;

/// <summary>
/// Adapts ESI research-agent transport models into character-domain source records.
/// </summary>
public sealed class EsiCharacterResearchAgentDataSource(
    IEsiClient esiClient) : ICharacterResearchAgentDataSource
{
    private readonly IEsiClient _esiClient = esiClient ?? throw new ArgumentNullException(nameof(esiClient));

    public async Task<Result<IReadOnlyList<CharacterResearchAgentData>>> GetResearchAgentsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<EsiResearchAgent>> researchAgents = await _esiClient
            .GetResearchAgentsAsync(characterId, cancellationToken)
            .ConfigureAwait(false);

        if (researchAgents.IsFailure)
        {
            return Result<IReadOnlyList<CharacterResearchAgentData>>.Failure(researchAgents.Error);
        }

        return Result<IReadOnlyList<CharacterResearchAgentData>>.Success(
            researchAgents.Value.Select(agent => new CharacterResearchAgentData(
                agent.AgentId,
                agent.SkillTypeId,
                agent.StartedAt,
                agent.PointsPerDay,
                agent.RemainderPoints)).ToList());
    }
}