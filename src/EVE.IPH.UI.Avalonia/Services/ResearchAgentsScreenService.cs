using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Characters.Services;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class ResearchAgentsScreenService : IResearchAgentsScreenService
{
    private readonly IPhase11SampleDataProvider _sampleDataProvider;
    private readonly IResearchAgentDatacoreService _researchAgentDatacoreService;
    private readonly ICharacterRepository? _characterRepository;
    private readonly IResearchAgentService? _researchAgentService;

    public ResearchAgentsScreenService(
        IPhase11SampleDataProvider sampleDataProvider,
        IResearchAgentDatacoreService researchAgentDatacoreService,
        ICharacterRepository? characterRepository = null,
        IResearchAgentService? researchAgentService = null)
    {
        _sampleDataProvider = sampleDataProvider ?? throw new ArgumentNullException(nameof(sampleDataProvider));
        _researchAgentDatacoreService = researchAgentDatacoreService ?? throw new ArgumentNullException(nameof(researchAgentDatacoreService));
        _characterRepository = characterRepository;
        _researchAgentService = researchAgentService;
    }

    public async Task<ResearchAgentsScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default)
    {
        if (_characterRepository is null || _researchAgentService is null)
        {
            return BuildScreenData([], "Character-backed research-agent services are not available in the current host configuration.");
        }

        Result<IReadOnlyList<CharacterRecord>> characters = await _characterRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        if (characters.IsFailure)
        {
            return BuildScreenData([], $"Unable to load stored characters for research agents: {characters.Error.Message}");
        }

        CharacterRecord? selectedCharacter = characters.Value.FirstOrDefault(character => character.IsDefault)
            ?? characters.Value.FirstOrDefault();

        if (selectedCharacter is null)
        {
            return BuildScreenData([], "No characters have been connected yet. Connect and sync a character to load current research agents.");
        }

        Result<IReadOnlyList<ResearchAgent>> researchAgents = await _researchAgentService
            .GetAsync(selectedCharacter.CharacterId, cancellationToken)
            .ConfigureAwait(false);

        if (researchAgents.IsFailure)
        {
            return BuildScreenData([], $"Unable to load research agents for {selectedCharacter.Name}: {researchAgents.Error.Message}");
        }

        if (researchAgents.Value.Count == 0)
        {
            return BuildScreenData([], $"No synced research-agent records were found for {selectedCharacter.Name} yet. Refresh the character to pull current datacore progress from ESI.");
        }

        return BuildScreenData(
            researchAgents.Value,
            $"Loaded research agents for {selectedCharacter.Name} from the local SQLite store. Datacore prices remain seeded until the market path is wired.");
    }

    private ResearchAgentsScreenData BuildScreenData(IReadOnlyList<ResearchAgent> researchAgents, string statusText) =>
        new(
            _researchAgentDatacoreService.BuildSummary(
                researchAgents,
                _sampleDataProvider.GetDatacorePrices(),
                _sampleDataProvider.GetDatacoreRedeemCost()),
            statusText);
}