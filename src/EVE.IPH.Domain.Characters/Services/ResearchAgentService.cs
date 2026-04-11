using System.Globalization;
using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Characters.Services;

/// <summary>
/// Coordinates repository-backed research-agent loading with external refreshes.
/// </summary>
public sealed class ResearchAgentService(
    ICharacterResearchAgentRepository characterResearchAgentRepository,
    IResearchAgentDefinitionRepository researchAgentDefinitionRepository,
    IItemRepository itemRepository,
    ICharacterResearchAgentDataSource characterResearchAgentDataSource,
    TimeProvider timeProvider) : IResearchAgentService
{
    private readonly ICharacterResearchAgentRepository _characterResearchAgentRepository = characterResearchAgentRepository ?? throw new ArgumentNullException(nameof(characterResearchAgentRepository));
    private readonly IResearchAgentDefinitionRepository _researchAgentDefinitionRepository = researchAgentDefinitionRepository ?? throw new ArgumentNullException(nameof(researchAgentDefinitionRepository));
    private readonly IItemRepository _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
    private readonly ICharacterResearchAgentDataSource _characterResearchAgentDataSource = characterResearchAgentDataSource ?? throw new ArgumentNullException(nameof(characterResearchAgentDataSource));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<Result<IReadOnlyList<ResearchAgent>>> GetAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<CharacterResearchAgentRecord>> storedAgents = await _characterResearchAgentRepository
            .GetByCharacterIdAsync(characterId, cancellationToken)
            .ConfigureAwait(false);

        if (storedAgents.IsFailure)
        {
            return Result<IReadOnlyList<ResearchAgent>>.Failure(storedAgents.Error);
        }

        return await MapAsync(storedAgents.Value, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<IReadOnlyList<ResearchAgent>>> RefreshAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<CharacterResearchAgentData>> currentAgents = await _characterResearchAgentDataSource
            .GetResearchAgentsAsync(characterId, cancellationToken)
            .ConfigureAwait(false);

        if (currentAgents.IsFailure)
        {
            return Result<IReadOnlyList<ResearchAgent>>.Failure(currentAgents.Error);
        }

        Result<IReadOnlyList<CharacterResearchAgentRecord>> storedAgents = await _characterResearchAgentRepository
            .ReplaceAsync(
                characterId,
                currentAgents.Value.Select(agent => new CharacterResearchAgentRecord(
                    characterId,
                    agent.AgentId,
                    agent.SkillTypeId,
                    agent.PointsPerDay,
                    agent.ResearchStartDate,
                    agent.RemainderPoints)).ToList(),
                cancellationToken)
            .ConfigureAwait(false);

        if (storedAgents.IsFailure)
        {
            return Result<IReadOnlyList<ResearchAgent>>.Failure(storedAgents.Error);
        }

        return await MapAsync(storedAgents.Value, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<IReadOnlyList<ResearchAgent>>> MapAsync(
        IReadOnlyList<CharacterResearchAgentRecord> records,
        CancellationToken cancellationToken)
    {
        if (records.Count == 0)
        {
            return Result<IReadOnlyList<ResearchAgent>>.Success([]);
        }

        Result<IReadOnlyDictionary<long, ResearchAgentDefinitionRecord>> definitions = await _researchAgentDefinitionRepository
            .GetByAgentIdsAsync(records.Select(record => record.AgentId).Distinct().ToArray(), cancellationToken)
            .ConfigureAwait(false);

        if (definitions.IsFailure)
        {
            return Result<IReadOnlyList<ResearchAgent>>.Failure(definitions.Error);
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        List<ResearchAgent> result = new(records.Count);

        foreach (CharacterResearchAgentRecord record in records)
        {
            Maybe<string> fieldName = await _itemRepository.GetItemNameAsync(record.SkillTypeId, cancellationToken).ConfigureAwait(false);
            ResearchAgentDefinitionRecord? definition = definitions.Value.GetValueOrDefault(record.AgentId);

            string agentName = definition?.AgentName ?? record.AgentId.ToString(CultureInfo.InvariantCulture);
            string field = fieldName.HasValue ? fieldName.Value : record.SkillTypeId.Value.ToString(CultureInfo.InvariantCulture);
            int agentLevel = definition?.Level ?? 0;
            string location = definition?.Location ?? string.Empty;
            double elapsedDays = Math.Max(0, (now - record.ResearchStartDate).TotalDays);
            double currentResearchPoints = (record.PointsPerDay * elapsedDays) + record.RemainderPoints;

            result.Add(new ResearchAgent(
                record.AgentId,
                record.SkillTypeId,
                agentName,
                field,
                currentResearchPoints,
                record.PointsPerDay,
                agentLevel,
                location,
                record.ResearchStartDate,
                record.RemainderPoints));
        }

        return Result<IReadOnlyList<ResearchAgent>>.Success(result);
    }
}