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
using EVE.IPH.Domain.Manufacturing.Services;
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
        ICorporationBlueprintService corporationBlueprintService = Substitute.For<ICorporationBlueprintService>();
        ICorporationCapabilityResolver corporationCapabilityResolver = Substitute.For<ICorporationCapabilityResolver>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        ICharacterService characterService = Substitute.For<ICharacterService>();
        ICharacterIndustryJobService characterIndustryJobService = Substitute.For<ICharacterIndustryJobService>();
        ICorporationIndustryJobService corporationIndustryJobService = Substitute.For<ICorporationIndustryJobService>();
        IIndustryJobRepository industryJobRepository = Substitute.For<IIndustryJobRepository>();
        IOwnedBlueprintRepository ownedBlueprintRepository = Substitute.For<IOwnedBlueprintRepository>();
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

        CharacterManagementService service = new(characterRepository, assetRepository, characterAssetService, corporationAssetService, corporationBlueprintService, corporationCapabilityResolver, corporationConnectionRepository, characterService, characterIndustryJobService, corporationIndustryJobService, industryJobRepository, ownedBlueprintRepository, esiClient, interactiveLoginService, researchAgentService, tokenStore);

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
        ICorporationBlueprintService corporationBlueprintService = Substitute.For<ICorporationBlueprintService>();
        ICorporationCapabilityResolver corporationCapabilityResolver = Substitute.For<ICorporationCapabilityResolver>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        ICharacterService characterService = Substitute.For<ICharacterService>();
        ICharacterIndustryJobService characterIndustryJobService = Substitute.For<ICharacterIndustryJobService>();
        ICorporationIndustryJobService corporationIndustryJobService = Substitute.For<ICorporationIndustryJobService>();
        IIndustryJobRepository industryJobRepository = Substitute.For<IIndustryJobRepository>();
        IOwnedBlueprintRepository ownedBlueprintRepository = Substitute.For<IOwnedBlueprintRepository>();
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

        CharacterManagementService service = new(characterRepository, assetRepository, characterAssetService, corporationAssetService, corporationBlueprintService, corporationCapabilityResolver, corporationConnectionRepository, characterService, characterIndustryJobService, corporationIndustryJobService, industryJobRepository, ownedBlueprintRepository, esiClient, interactiveLoginService, researchAgentService, tokenStore);

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
        ICorporationBlueprintService corporationBlueprintService = Substitute.For<ICorporationBlueprintService>();
        ICorporationCapabilityResolver corporationCapabilityResolver = Substitute.For<ICorporationCapabilityResolver>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        ICharacterService characterService = Substitute.For<ICharacterService>();
        ICharacterIndustryJobService characterIndustryJobService = Substitute.For<ICharacterIndustryJobService>();
        ICorporationIndustryJobService corporationIndustryJobService = Substitute.For<ICorporationIndustryJobService>();
        IIndustryJobRepository industryJobRepository = Substitute.For<IIndustryJobRepository>();
        IOwnedBlueprintRepository ownedBlueprintRepository = Substitute.For<IOwnedBlueprintRepository>();
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

        CharacterManagementService service = new(characterRepository, assetRepository, characterAssetService, corporationAssetService, corporationBlueprintService, corporationCapabilityResolver, corporationConnectionRepository, characterService, characterIndustryJobService, corporationIndustryJobService, industryJobRepository, ownedBlueprintRepository, esiClient, interactiveLoginService, researchAgentService, tokenStore);

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
        ICorporationBlueprintService corporationBlueprintService = Substitute.For<ICorporationBlueprintService>();
        ICorporationCapabilityResolver corporationCapabilityResolver = Substitute.For<ICorporationCapabilityResolver>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        ICharacterService characterService = Substitute.For<ICharacterService>();
        ICharacterIndustryJobService characterIndustryJobService = Substitute.For<ICharacterIndustryJobService>();
        ICorporationIndustryJobService corporationIndustryJobService = Substitute.For<ICorporationIndustryJobService>();
        IIndustryJobRepository industryJobRepository = Substitute.For<IIndustryJobRepository>();
        IOwnedBlueprintRepository ownedBlueprintRepository = Substitute.For<IOwnedBlueprintRepository>();
        IEsiClient esiClient = Substitute.For<IEsiClient>();
        IEsiInteractiveLoginService interactiveLoginService = Substitute.For<IEsiInteractiveLoginService>();
        IResearchAgentService researchAgentService = Substitute.For<IResearchAgentService>();
        IEsiTokenStore tokenStore = Substitute.For<IEsiTokenStore>();

        CharacterManagementService service = new(characterRepository, assetRepository, characterAssetService, corporationAssetService, corporationBlueprintService, corporationCapabilityResolver, corporationConnectionRepository, characterService, characterIndustryJobService, corporationIndustryJobService, industryJobRepository, ownedBlueprintRepository, esiClient, interactiveLoginService, researchAgentService, tokenStore);

        Result<IReadOnlyList<CharacterRecord>> result = await service.DeleteAsync(SpecialCharacters.AllSkillsVId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CHARACTER_DELETE_NOT_SUPPORTED");
        await characterRepository.DidNotReceiveWithAnyArgs().DeleteAsync(default, default);
    }

    [Fact]
    public async Task GetScreenDataAsync_WhenTokensExist_ReportsHealthPerCharacter()
    {
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        IEsiTokenStore tokenStore = Substitute.For<IEsiTokenStore>();

        CharacterRecord character = CreateCharacter(new CharacterId(90000001), true);
        characterRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success([character])));
        corporationConnectionRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CorporationConnectionRecord>>.Success([])));
        tokenStore.ReadAsync(character.CharacterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Maybe<EsiTokenRecord>.Some(new EsiTokenRecord(
                "access",
                "refresh",
                DateTimeOffset.UtcNow.AddMinutes(30),
                ["esi-skills.read_skills.v1"],
                Maybe<CharacterId>.Some(character.CharacterId)))));

        CharacterManagementQueryService service = new(characterRepository, corporationConnectionRepository, tokenStore);

        Result<CharacterManagementScreenData> result = await service.GetScreenDataAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Characters.Should().ContainSingle();
        result.Value.Characters[0].TokenStatus.HasStoredToken.Should().BeTrue();
        result.Value.Characters[0].TokenStatus.IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectCorporationAsync_WhenCharacterHasCorporateScopes_PersistsConnectionAndRefreshesData()
    {
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        IAssetRepository assetRepository = Substitute.For<IAssetRepository>();
        ICharacterAssetService characterAssetService = Substitute.For<ICharacterAssetService>();
        ICorporationAssetService corporationAssetService = Substitute.For<ICorporationAssetService>();
        ICorporationBlueprintService corporationBlueprintService = Substitute.For<ICorporationBlueprintService>();
        ICorporationCapabilityResolver corporationCapabilityResolver = Substitute.For<ICorporationCapabilityResolver>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        ICharacterService characterService = Substitute.For<ICharacterService>();
        ICharacterIndustryJobService characterIndustryJobService = Substitute.For<ICharacterIndustryJobService>();
        ICorporationIndustryJobService corporationIndustryJobService = Substitute.For<ICorporationIndustryJobService>();
        IIndustryJobRepository industryJobRepository = Substitute.For<IIndustryJobRepository>();
        IOwnedBlueprintRepository ownedBlueprintRepository = Substitute.For<IOwnedBlueprintRepository>();
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
                ["esi-assets.read_corporation_assets", "esi-industry.read_corporation_jobs", "esi-corporations.read_blueprints", "esi-corporations.read_corporation_membership"],
                Maybe<CharacterId>.Some(characterId)))));
        corporationCapabilityResolver.ResolveAsync(corporationId, characterId, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CorporationCapabilityState>.Success(new CorporationCapabilityState(
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true))));
        esiClient.GetNamesAsync(Arg.Is<IReadOnlyList<long>>(ids => ids.Count == 1 && ids[0] == corporationId.Value), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<EVE.IPH.Infrastructure.ESI.Models.EsiEntityName>>.Success([
                new EVE.IPH.Infrastructure.ESI.Models.EsiEntityName(corporationId.Value, "corporation", "Acme Holdings"),
            ])));
        corporationConnectionRepository.UpsertAsync(Arg.Any<CorporationConnectionRecord>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Result<CorporationConnectionRecord>.Success(call.Arg<CorporationConnectionRecord>())));
        corporationAssetService.RefreshAsync(corporationId, characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<AssetRecord>>.Success([])));
        corporationBlueprintService.RefreshAsync(corporationId, characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<OwnedBlueprintRecord>>.Success([])));
        corporationIndustryJobService.RefreshAsync(corporationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CorporationIndustryJobSnapshot>.Success(new CorporationIndustryJobSnapshot(corporationId, [], new IndustryJobSummary(0, 0, 0, 0, 0, 0)))));

        CharacterManagementService service = new(characterRepository, assetRepository, characterAssetService, corporationAssetService, corporationBlueprintService, corporationCapabilityResolver, corporationConnectionRepository, characterService, characterIndustryJobService, corporationIndustryJobService, industryJobRepository, ownedBlueprintRepository, esiClient, interactiveLoginService, researchAgentService, tokenStore);

        Result<CorporationConnectionRecord> result = await service.ConnectCorporationAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Acme Holdings");
        result.Value.HasDirectorRole.Should().BeTrue();
        result.Value.HasFactoryManagerRole.Should().BeTrue();
        await corporationConnectionRepository.Received(1).UpsertAsync(Arg.Is<CorporationConnectionRecord>(record => record.CorporationId == corporationId && record.HasAssetAccess && record.HasIndustryJobAccess && record.HasDirectorRole && record.HasFactoryManagerRole), Arg.Any<CancellationToken>());
        await corporationAssetService.Received(1).RefreshAsync(corporationId, characterId, Arg.Any<CancellationToken>());
        await corporationBlueprintService.Received(1).RefreshAsync(corporationId, characterId, Arg.Any<CancellationToken>());
        await corporationIndustryJobService.Received(1).RefreshAsync(corporationId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetScreenDataAsync_WhenRealCharacterTokenMissing_AppendsTokenWarningToStatusText()
    {
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        IEsiTokenStore tokenStore = Substitute.For<IEsiTokenStore>();

        CharacterRecord character = CreateCharacter(new CharacterId(90000001), true);
        characterRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success([character])));
        corporationConnectionRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CorporationConnectionRecord>>.Success([])));
        tokenStore.ReadAsync(character.CharacterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Maybe<EsiTokenRecord>.None));

        CharacterManagementQueryService service = new(characterRepository, corporationConnectionRepository, tokenStore);

        Result<CharacterManagementScreenData> result = await service.GetScreenDataAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.StatusText.Should().Contain("Token status warning: 1 connected character needs re-authentication.");
    }

    [Fact]
    public async Task GetScreenDataAsync_WhenCorporationHasNoScopedAccess_AppendsCorporationWarningToStatusText()
    {
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        IEsiTokenStore tokenStore = Substitute.For<IEsiTokenStore>();

        CharacterId characterId = new(90000001);
        CharacterRecord character = CreateCharacter(characterId, true);
        CorporationConnectionRecord corporation = new(new CorporationId(98000001), "Acme Holdings", characterId, false, false, false);

        characterRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success([character])));
        corporationConnectionRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CorporationConnectionRecord>>.Success([corporation])));
        tokenStore.ReadAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Maybe<EsiTokenRecord>.Some(new EsiTokenRecord(
                "access",
                "refresh",
                DateTimeOffset.UtcNow.AddMinutes(30),
                ["esi-skills.read_skills.v1"],
                Maybe<CharacterId>.Some(characterId)))));

        CharacterManagementQueryService service = new(characterRepository, corporationConnectionRepository, tokenStore);

        Result<CharacterManagementScreenData> result = await service.GetScreenDataAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.StatusText.Should().Contain("Corporation access warning: 1 corporation connection has no scoped access yet.");
    }

    [Fact]
    public async Task ConnectCorporationAsync_WhenMembershipScopeMissing_ReturnsFailure()
    {
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        IAssetRepository assetRepository = Substitute.For<IAssetRepository>();
        ICharacterAssetService characterAssetService = Substitute.For<ICharacterAssetService>();
        ICorporationAssetService corporationAssetService = Substitute.For<ICorporationAssetService>();
        ICorporationBlueprintService corporationBlueprintService = Substitute.For<ICorporationBlueprintService>();
        ICorporationCapabilityResolver corporationCapabilityResolver = Substitute.For<ICorporationCapabilityResolver>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        ICharacterService characterService = Substitute.For<ICharacterService>();
        ICharacterIndustryJobService characterIndustryJobService = Substitute.For<ICharacterIndustryJobService>();
        ICorporationIndustryJobService corporationIndustryJobService = Substitute.For<ICorporationIndustryJobService>();
        IIndustryJobRepository industryJobRepository = Substitute.For<IIndustryJobRepository>();
        IOwnedBlueprintRepository ownedBlueprintRepository = Substitute.For<IOwnedBlueprintRepository>();
        IEsiClient esiClient = Substitute.For<IEsiClient>();
        IEsiInteractiveLoginService interactiveLoginService = Substitute.For<IEsiInteractiveLoginService>();
        IResearchAgentService researchAgentService = Substitute.For<IResearchAgentService>();
        IEsiTokenStore tokenStore = Substitute.For<IEsiTokenStore>();

        CharacterId characterId = new(90000001);
        CharacterRecord character = CreateCharacter(characterId, true);
        CorporationId corporationId = character.CorporationId;

        characterRepository.GetByIdAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Maybe<CharacterRecord>.Some(character)));
        tokenStore.ReadAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Maybe<EsiTokenRecord>.Some(new EsiTokenRecord(
                "access",
                "refresh",
                DateTimeOffset.UtcNow.AddHours(1),
                ["esi-assets.read_corporation_assets"],
                Maybe<CharacterId>.Some(characterId)))));
        corporationCapabilityResolver.ResolveAsync(corporationId, characterId, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CorporationCapabilityState>.Success(new CorporationCapabilityState(
                false,
                true,
                false,
                false,
                false,
                false,
                false,
                false,
                false))));

        CharacterManagementService service = new(characterRepository, assetRepository, characterAssetService, corporationAssetService, corporationBlueprintService, corporationCapabilityResolver, corporationConnectionRepository, characterService, characterIndustryJobService, corporationIndustryJobService, industryJobRepository, ownedBlueprintRepository, esiClient, interactiveLoginService, researchAgentService, tokenStore);

        Result<CorporationConnectionRecord> result = await service.ConnectCorporationAsync(characterId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CORPORATION_MEMBERSHIP_SCOPE_MISSING");
        await corporationConnectionRepository.DidNotReceiveWithAnyArgs().UpsertAsync(default!, default);
    }

    [Fact]
    public async Task RefreshCorporationAsync_WhenCapabilitiesAreLost_DowngradesConnectionAndClearsStoredData()
    {
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        IAssetRepository assetRepository = Substitute.For<IAssetRepository>();
        ICharacterAssetService characterAssetService = Substitute.For<ICharacterAssetService>();
        ICorporationAssetService corporationAssetService = Substitute.For<ICorporationAssetService>();
        ICorporationBlueprintService corporationBlueprintService = Substitute.For<ICorporationBlueprintService>();
        ICorporationCapabilityResolver corporationCapabilityResolver = Substitute.For<ICorporationCapabilityResolver>();
        ICorporationConnectionRepository corporationConnectionRepository = Substitute.For<ICorporationConnectionRepository>();
        ICharacterService characterService = Substitute.For<ICharacterService>();
        ICharacterIndustryJobService characterIndustryJobService = Substitute.For<ICharacterIndustryJobService>();
        ICorporationIndustryJobService corporationIndustryJobService = Substitute.For<ICorporationIndustryJobService>();
        IIndustryJobRepository industryJobRepository = Substitute.For<IIndustryJobRepository>();
        IOwnedBlueprintRepository ownedBlueprintRepository = Substitute.For<IOwnedBlueprintRepository>();
        IEsiClient esiClient = Substitute.For<IEsiClient>();
        IEsiInteractiveLoginService interactiveLoginService = Substitute.For<IEsiInteractiveLoginService>();
        IResearchAgentService researchAgentService = Substitute.For<IResearchAgentService>();
        IEsiTokenStore tokenStore = Substitute.For<IEsiTokenStore>();

        CharacterId characterId = new(90000001);
        CharacterRecord character = CreateCharacter(characterId, true);
        CorporationId corporationId = character.CorporationId;
        CorporationConnectionRecord existingConnection = new(corporationId, "Acme Holdings", characterId, true, true, false, true, true);

        corporationConnectionRepository.GetByIdAsync(corporationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Maybe<CorporationConnectionRecord>.Some(existingConnection)));
        tokenStore.ReadAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Maybe<EsiTokenRecord>.Some(new EsiTokenRecord(
                "access",
                "refresh",
                DateTimeOffset.UtcNow.AddHours(1),
                ["esi-assets.read_corporation_assets", "esi-industry.read_corporation_jobs", "esi-corporations.read_corporation_membership"],
                Maybe<CharacterId>.Some(characterId)))));
        corporationCapabilityResolver.ResolveAsync(corporationId, characterId, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CorporationCapabilityState>.Success(new CorporationCapabilityState(
                true,
                true,
                true,
                false,
                false,
                false,
                false,
                false,
                false))));
        assetRepository.DeleteByOwnerIdAsync(corporationId.Value, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<bool>.Success(true)));
        ownedBlueprintRepository.DeleteByUserAsync(corporationId.Value, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<bool>.Success(true)));
        characterRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CharacterRecord>>.Success([character])));
        industryJobRepository.ReplaceAsync(characterId, IndustryJobScope.Corporation, Arg.Any<IReadOnlyList<IndustryJobRecord>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<IndustryJobRecord>>.Success([])));
        corporationConnectionRepository.UpsertAsync(Arg.Any<CorporationConnectionRecord>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Result<CorporationConnectionRecord>.Success(call.Arg<CorporationConnectionRecord>())));

        CharacterManagementService service = new(characterRepository, assetRepository, characterAssetService, corporationAssetService, corporationBlueprintService, corporationCapabilityResolver, corporationConnectionRepository, characterService, characterIndustryJobService, corporationIndustryJobService, industryJobRepository, ownedBlueprintRepository, esiClient, interactiveLoginService, researchAgentService, tokenStore);

        Result<CorporationConnectionRecord> result = await service.RefreshCorporationAsync(corporationId);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasAssetAccess.Should().BeFalse();
        result.Value.HasIndustryJobAccess.Should().BeFalse();
        result.Value.HasDirectorRole.Should().BeFalse();
        result.Value.HasFactoryManagerRole.Should().BeFalse();
        await assetRepository.Received(1).DeleteByOwnerIdAsync(corporationId.Value, Arg.Any<CancellationToken>());
        await ownedBlueprintRepository.Received(1).DeleteByUserAsync(corporationId.Value, Arg.Any<CancellationToken>());
        await industryJobRepository.Received(1).ReplaceAsync(characterId, IndustryJobScope.Corporation, Arg.Is<IReadOnlyList<IndustryJobRecord>>(jobs => jobs.Count == 0), Arg.Any<CancellationToken>());
    }

    private static CharacterRecord CreateCharacter(CharacterId characterId, bool isDefault, string? name = null) => new(
        characterId,
        name ?? "Kara Maken",
        characterId == SpecialCharacters.AllSkillsVId ? SpecialCharacters.PlaceholderCorporationId : new CorporationId(98000001),
        Maybe<AllianceId>.None,
        isDefault);
}