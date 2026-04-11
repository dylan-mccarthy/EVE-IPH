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
        if (_characterRepository is not null && _researchAgentService is not null)
        {
            Result<IReadOnlyList<CharacterRecord>> characters = await _characterRepository
                .GetAllAsync(cancellationToken)
                .ConfigureAwait(false);

            if (characters.IsSuccess)
            {
                CharacterRecord? selectedCharacter = characters.Value.FirstOrDefault(character => character.IsDefault)
                    ?? characters.Value.FirstOrDefault();

                if (selectedCharacter is not null)
                {
                    Result<IReadOnlyList<ResearchAgent>> researchAgents = await _researchAgentService
                        .GetAsync(selectedCharacter.CharacterId, cancellationToken)
                        .ConfigureAwait(false);

                    if (researchAgents.IsSuccess)
                    {
                        return BuildScreenData(
                            researchAgents.Value,
                            $"Loaded research agents for {selectedCharacter.Name} from the existing SQLite database. Datacore prices remain seeded until the market path is wired.");
                    }

                    return BuildSampleScreenData($"Fell back to seeded research agents because stored data could not be loaded: {researchAgents.Error.Message}");
                }

                return BuildSampleScreenData("No stored characters were found in the SQLite database. Showing seeded research-agent data.");
            }

            return BuildSampleScreenData($"Fell back to seeded research agents because the character list could not be loaded: {characters.Error.Message}");
        }

        return BuildSampleScreenData("Legacy SQLite database not found. Showing seeded research-agent data.");
    }

    private ResearchAgentsScreenData BuildSampleScreenData(string statusText) =>
        BuildScreenData(_sampleDataProvider.GetResearchAgents(), statusText);

    private ResearchAgentsScreenData BuildScreenData(IReadOnlyList<ResearchAgent> researchAgents, string statusText) =>
        new(
            _researchAgentDatacoreService.BuildSummary(
                researchAgents,
                _sampleDataProvider.GetDatacorePrices(),
                _sampleDataProvider.GetDatacoreRedeemCost()),
            statusText);
}