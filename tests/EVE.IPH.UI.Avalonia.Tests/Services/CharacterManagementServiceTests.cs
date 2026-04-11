using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Assets.Services;
using EVE.IPH.Domain.Characters.Services;
using EVE.IPH.Domain.Core;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Industry.Models;
using EVE.IPH.Domain.Industry.Services;
using EVE.IPH.Infrastructure.ESI;
using EVE.IPH.Infrastructure.ESI.Interfaces;
using EVE.IPH.UI.Avalonia.Services;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.Services;

public sealed class CharacterManagementServiceTests
{
    [Fact]
    public async Task AuthenticateAndRefreshAsync_WhenCharacterRefreshSucceeds_AlsoRefreshesResearchAgents()
    {
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        IAssetRepository assetRepository = Substitute.For<IAssetRepository>();
        ICharacterAssetService characterAssetService = Substitute.For<ICharacterAssetService>();
        ICorporationAssetService corporationAssetService = Substitute.For<ICorporationAssetService>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        ICharacterService characterService = Substitute.For<ICharacterService>();
        ICharacterIndustryJobService characterIndustryJobService = Substitute.For<ICharacterIndustryJobService>();
        ICorporationIndustryJobService corporationIndustryJobService = Substitute.For<ICorporationIndustryJobService>();
        IEsiClient esiClient = Substitute.For<IEsiClient>();
        IEsiInteractiveLoginService interactiveLoginService = Substitute.For<IEsiInteractiveLoginService>();
        IResearchAgentService researchAgentService = Substitute.For<IResearchAgentService>();
        IEsiTokenStore tokenStore = Substitute.For<IEsiTokenStore>();

        CharacterId characterId = new(90000001);
        CharacterRecord character = CreateCharacter(characterId, true);

        interactiveLoginService.AuthenticateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<EsiAccessToken>.Success(new EsiAccessToken(
                "access",
                "refresh",
                DateTimeOffset.UtcNow.AddHours(1),
                [],
                Maybe<CharacterId>.Some(characterId)))));
        characterRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success(Array.Empty<CharacterRecord>())),
                Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success([character])));
        characterService.RefreshAsync(characterId, true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CharacterSnapshot>.Success(new CharacterSnapshot(character, [], []))));
        characterAssetService.RefreshAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<AssetRecord>>.Success([])));
        characterIndustryJobService.RefreshAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IndustryJobSnapshot>.Success(new IndustryJobSnapshot(characterId, [], new IndustryJobSummary(0, 0, 0, 0, 0, 0)))));
        researchAgentService.RefreshAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<ResearchAgent>>.Success([])));

        CharacterManagementService service = new(characterRepository, assetRepository, characterAssetService, corporationAssetService, corporationConnectionRepository, characterService, characterIndustryJobService, corporationIndustryJobService, esiClient, interactiveLoginService, researchAgentService, tokenStore);

        Result<CharacterRecord> result = await service.AuthenticateAndRefreshAsync();

        result.IsSuccess.Should().BeTrue();
        await characterAssetService.Received(1).RefreshAsync(characterId, Arg.Any<CancellationToken>());
        await researchAgentService.Received(1).RefreshAsync(characterId, Arg.Any<CancellationToken>());
        await characterIndustryJobService.Received(1).RefreshAsync(characterId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_WhenCharacterExists_AlsoRefreshesResearchAgents()
    {
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        IAssetRepository assetRepository = Substitute.For<IAssetRepository>();
        ICharacterAssetService characterAssetService = Substitute.For<ICharacterAssetService>();
        ICorporationAssetService corporationAssetService = Substitute.For<ICorporationAssetService>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        ICharacterService characterService = Substitute.For<ICharacterService>();
        ICharacterIndustryJobService characterIndustryJobService = Substitute.For<ICharacterIndustryJobService>();
        ICorporationIndustryJobService corporationIndustryJobService = Substitute.For<ICorporationIndustryJobService>();
        IEsiClient esiClient = Substitute.For<IEsiClient>();
        IEsiInteractiveLoginService interactiveLoginService = Substitute.For<IEsiInteractiveLoginService>();
        IResearchAgentService researchAgentService = Substitute.For<IResearchAgentService>();
        IEsiTokenStore tokenStore = Substitute.For<IEsiTokenStore>();

        CharacterId characterId = new(90000001);
        CharacterRecord character = CreateCharacter(characterId, true);

        characterRepository.GetByIdAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Maybe<CharacterRecord>.Some(character)));
        characterService.RefreshAsync(characterId, true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CharacterSnapshot>.Success(new CharacterSnapshot(character, [], []))));
        characterAssetService.RefreshAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<AssetRecord>>.Success([])));
        characterIndustryJobService.RefreshAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IndustryJobSnapshot>.Success(new IndustryJobSnapshot(characterId, [], new IndustryJobSummary(0, 0, 0, 0, 0, 0)))));
        researchAgentService.RefreshAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<ResearchAgent>>.Success([])));

        CharacterManagementService service = new(characterRepository, assetRepository, characterAssetService, corporationAssetService, corporationConnectionRepository, characterService, characterIndustryJobService, corporationIndustryJobService, esiClient, interactiveLoginService, researchAgentService, tokenStore);

        Result<CharacterRecord> result = await service.RefreshAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        await characterAssetService.Received(1).RefreshAsync(characterId, Arg.Any<CancellationToken>());
        await researchAgentService.Received(1).RefreshAsync(characterId, Arg.Any<CancellationToken>());
        await characterIndustryJobService.Received(1).RefreshAsync(characterId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthenticateAndRefreshAsync_WhenOnlyPlaceholderExists_MakesRealCharacterDefault()
    {
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        IAssetRepository assetRepository = Substitute.For<IAssetRepository>();
        ICharacterAssetService characterAssetService = Substitute.For<ICharacterAssetService>();
        ICorporationAssetService corporationAssetService = Substitute.For<ICorporationAssetService>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        ICharacterService characterService = Substitute.For<ICharacterService>();
        ICharacterIndustryJobService characterIndustryJobService = Substitute.For<ICharacterIndustryJobService>();
        ICorporationIndustryJobService corporationIndustryJobService = Substitute.For<ICorporationIndustryJobService>();
        IEsiClient esiClient = Substitute.For<IEsiClient>();
        IEsiInteractiveLoginService interactiveLoginService = Substitute.For<IEsiInteractiveLoginService>();
        IResearchAgentService researchAgentService = Substitute.For<IResearchAgentService>();
        IEsiTokenStore tokenStore = Substitute.For<IEsiTokenStore>();

        CharacterRecord placeholder = CreateCharacter(SpecialCharacters.AllSkillsVId, true, SpecialCharacters.AllSkillsVName);
        CharacterId characterId = new(90000001);
        CharacterRecord realCharacter = CreateCharacter(characterId, true);

        interactiveLoginService.AuthenticateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<EsiAccessToken>.Success(new EsiAccessToken(
                "access",
                "refresh",
                DateTimeOffset.UtcNow.AddHours(1),
                [],
                Maybe<CharacterId>.Some(characterId)))));
        characterRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success([placeholder])),
                Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success([placeholder, realCharacter])));
        characterService.RefreshAsync(characterId, true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CharacterSnapshot>.Success(new CharacterSnapshot(realCharacter, [], []))));
        characterAssetService.RefreshAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<AssetRecord>>.Success([])));
        characterIndustryJobService.RefreshAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IndustryJobSnapshot>.Success(new IndustryJobSnapshot(characterId, [], new IndustryJobSummary(0, 0, 0, 0, 0, 0)))));
        researchAgentService.RefreshAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<ResearchAgent>>.Success([])));
        characterRepository.UpsertAsync(Arg.Is<CharacterRecord>(character => character.CharacterId == SpecialCharacters.AllSkillsVId && !character.IsDefault), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CharacterRecord>.Success(placeholder with { IsDefault = false })));

        CharacterManagementService service = new(characterRepository, assetRepository, characterAssetService, corporationAssetService, corporationConnectionRepository, characterService, characterIndustryJobService, corporationIndustryJobService, esiClient, interactiveLoginService, researchAgentService, tokenStore);

        Result<CharacterRecord> result = await service.AuthenticateAndRefreshAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.CharacterId.Should().Be(characterId);
        result.Value.IsDefault.Should().BeTrue();
        await characterRepository.Received(1).UpsertAsync(
            Arg.Is<CharacterRecord>(character => character.CharacterId == SpecialCharacters.AllSkillsVId && !character.IsDefault),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenPlaceholderRequested_ReturnsFailure()
    {
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        IAssetRepository assetRepository = Substitute.For<IAssetRepository>();
        ICharacterAssetService characterAssetService = Substitute.For<ICharacterAssetService>();
        ICorporationAssetService corporationAssetService = Substitute.For<ICorporationAssetService>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        ICharacterService characterService = Substitute.For<ICharacterService>();
        ICharacterIndustryJobService characterIndustryJobService = Substitute.For<ICharacterIndustryJobService>();
        ICorporationIndustryJobService corporationIndustryJobService = Substitute.For<ICorporationIndustryJobService>();
        IEsiClient esiClient = Substitute.For<IEsiClient>();
        IEsiInteractiveLoginService interactiveLoginService = Substitute.For<IEsiInteractiveLoginService>();
        IResearchAgentService researchAgentService = Substitute.For<IResearchAgentService>();
        IEsiTokenStore tokenStore = Substitute.For<IEsiTokenStore>();

        CharacterManagementService service = new(characterRepository, assetRepository, characterAssetService, corporationAssetService, corporationConnectionRepository, characterService, characterIndustryJobService, corporationIndustryJobService, esiClient, interactiveLoginService, researchAgentService, tokenStore);

        Result<IReadOnlyList<CharacterRecord>> result = await service.DeleteAsync(SpecialCharacters.AllSkillsVId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CHARACTER_DELETE_NOT_SUPPORTED");
        await characterRepository.DidNotReceiveWithAnyArgs().DeleteAsync(default, default);
    }

    [Fact]
    public async Task GetCharacterTokenStatusesAsync_WhenTokensExist_ReportsHealthPerCharacter()
    {
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        IAssetRepository assetRepository = Substitute.For<IAssetRepository>();
        ICharacterAssetService characterAssetService = Substitute.For<ICharacterAssetService>();
        ICorporationAssetService corporationAssetService = Substitute.For<ICorporationAssetService>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        ICharacterService characterService = Substitute.For<ICharacterService>();
        ICharacterIndustryJobService characterIndustryJobService = Substitute.For<ICharacterIndustryJobService>();
        ICorporationIndustryJobService corporationIndustryJobService = Substitute.For<ICorporationIndustryJobService>();
        IEsiClient esiClient = Substitute.For<IEsiClient>();
        IEsiInteractiveLoginService interactiveLoginService = Substitute.For<IEsiInteractiveLoginService>();
        IResearchAgentService researchAgentService = Substitute.For<IResearchAgentService>();
        IEsiTokenStore tokenStore = Substitute.For<IEsiTokenStore>();

        CharacterRecord character = CreateCharacter(new CharacterId(90000001), true);
        characterRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success([character])));
        tokenStore.ReadAsync(character.CharacterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Maybe<EsiTokenRecord>.Some(new EsiTokenRecord(
                "access",
                "refresh",
                DateTimeOffset.UtcNow.AddMinutes(30),
                ["esi-skills.read_skills.v1"],
                Maybe<CharacterId>.Some(character.CharacterId)))));

        CharacterManagementService service = new(characterRepository, assetRepository, characterAssetService, corporationAssetService, corporationConnectionRepository, characterService, characterIndustryJobService, corporationIndustryJobService, esiClient, interactiveLoginService, researchAgentService, tokenStore);

        Result<IReadOnlyList<CharacterTokenStatus>> result = await service.GetCharacterTokenStatusesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].HasStoredToken.Should().BeTrue();
        result.Value[0].IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectCorporationAsync_WhenCharacterHasCorporateScopes_PersistsConnectionAndRefreshesData()
    {
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        IAssetRepository assetRepository = Substitute.For<IAssetRepository>();
        ICharacterAssetService characterAssetService = Substitute.For<ICharacterAssetService>();
        ICorporationAssetService corporationAssetService = Substitute.For<ICorporationAssetService>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        ICharacterService characterService = Substitute.For<ICharacterService>();
        ICharacterIndustryJobService characterIndustryJobService = Substitute.For<ICharacterIndustryJobService>();
        ICorporationIndustryJobService corporationIndustryJobService = Substitute.For<ICorporationIndustryJobService>();
        IEsiClient esiClient = Substitute.For<IEsiClient>();
        IEsiInteractiveLoginService interactiveLoginService = Substitute.For<IEsiInteractiveLoginService>();
        IResearchAgentService researchAgentService = Substitute.For<IResearchAgentService>();
        IEsiTokenStore tokenStore = Substitute.For<IEsiTokenStore>();

        CharacterId characterId = new(90000001);
        CharacterRecord character = CreateCharacter(characterId, true);
        CorporationId corporationId = new(98000001);

        characterRepository.GetByIdAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Maybe<CharacterRecord>.Some(character)));
        tokenStore.ReadAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Maybe<EsiTokenRecord>.Some(new EsiTokenRecord(
                "access",
                "refresh",
                DateTimeOffset.UtcNow.AddHours(1),
                ["esi-assets.read_corporation_assets", "esi-industry.read_corporation_jobs", "esi-corporations.read_blueprints"],
                Maybe<CharacterId>.Some(characterId)))));
        esiClient.GetNamesAsync(Arg.Is<IReadOnlyList<long>>(ids => ids.Count == 1 && ids[0] == corporationId.Value), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<EVE.IPH.Infrastructure.ESI.Models.EsiEntityName>>.Success([
                new EVE.IPH.Infrastructure.ESI.Models.EsiEntityName(corporationId.Value, "corporation", "Acme Holdings"),
            ])));
        corporationConnectionRepository.UpsertAsync(Arg.Any<CorporationConnectionRecord>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Result<CorporationConnectionRecord>.Success(call.Arg<CorporationConnectionRecord>())));
        corporationAssetService.RefreshAsync(corporationId, characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<AssetRecord>>.Success([])));

        CharacterManagementService service = new(characterRepository, assetRepository, characterAssetService, corporationAssetService, corporationConnectionRepository, characterService, characterIndustryJobService, corporationIndustryJobService, esiClient, interactiveLoginService, researchAgentService, tokenStore);

        Result<CorporationConnectionRecord> result = await service.ConnectCorporationAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Acme Holdings");
        await corporationConnectionRepository.Received(1).UpsertAsync(Arg.Is<CorporationConnectionRecord>(record => record.CorporationId == corporationId && record.HasAssetAccess), Arg.Any<CancellationToken>());
        await corporationAssetService.Received(1).RefreshAsync(corporationId, characterId, Arg.Any<CancellationToken>());
    }

    private static CharacterRecord CreateCharacter(CharacterId characterId, bool isDefault, string? name = null) => new(
        characterId,
        name ?? "Kara Maken",
        characterId == SpecialCharacters.AllSkillsVId ? SpecialCharacters.PlaceholderCorporationId : new CorporationId(98000001),
        Maybe<AllianceId>.None,
        isDefault);
}