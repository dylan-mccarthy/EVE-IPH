using Dapper;
using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Assets.Services;
using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Characters.Services;
using EVE.IPH.Domain.Core;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Industry.Models;
using EVE.IPH.Domain.Industry.Services;
using EVE.IPH.Domain.Manufacturing.Services;
using EVE.IPH.Infrastructure.Data.Repositories.App;
using EVE.IPH.Infrastructure.ESI;
using EVE.IPH.Infrastructure.ESI.Interfaces;
using EVE.IPH.Infrastructure.ESI.Models;
using EVE.IPH.UI.Avalonia.Services;
using NSubstitute;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class CharacterManagementServiceIntegrationTests : IDisposable
{
    private readonly ServiceHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public async Task AuthenticateAndRefreshAsync_PersistsCharacterAssetsAndIndustryJobs()
    {
        CharacterId characterId = new(90000001);
        CharacterRecord character = CreateCharacter(characterId, "Kara Maken", isDefault: true);

        _harness.InteractiveLoginService.AuthenticateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<EsiAccessToken>.Success(new EsiAccessToken(
                "access",
                "refresh",
                DateTimeOffset.UtcNow.AddHours(1),
                ["esi-skills.read_skills.v1"],
                Maybe<CharacterId>.Some(characterId)))));

        _harness.CharacterService.RefreshAsync(characterId, true, Arg.Any<CancellationToken>())
            .Returns(_ => PersistCharacterSnapshotAsync(_harness.CharacterRepository, character));
        _harness.CharacterAssetService.RefreshAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(_ => PersistCharacterAssetsAsync(_harness.AssetRepository, characterId, [CreateAsset(characterId.Value, 7100001, 34, "Tritanium") ]));
        _harness.CharacterIndustryJobService.RefreshAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(_ => PersistCharacterJobsAsync(_harness.IndustryJobRepository, characterId, [CreateJob(42, characterId, IndustryJobScope.Personal, 1)]));
        _harness.ResearchAgentService.RefreshAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<ResearchAgent>>.Success([])));

        Result<CharacterRecord> result = await _harness.Service.AuthenticateAndRefreshAsync();
        Result<IReadOnlyList<CharacterRecord>> storedCharacters = await _harness.CharacterRepository.GetAllAsync();
        Result<IReadOnlyList<StoredAssetRecord>> storedAssets = await _harness.AssetRepository.GetByOwnerIdAsync(characterId.Value);
        Result<IReadOnlyList<IndustryJobRecord>> storedJobs = await _harness.IndustryJobRepository.GetByInstallerIdAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        storedCharacters.Value.Should().ContainSingle(record => record.CharacterId == characterId);
        storedAssets.Value.Should().ContainSingle(asset => asset.ItemName == "Tritanium");
        storedJobs.Value.Should().ContainSingle(job => job.Scope == IndustryJobScope.Personal && job.JobId == 42);
    }

    [Fact]
    public async Task ConnectCorporationAsync_PersistsCorporationAndSupportsMixedOwnerQueries()
    {
        CharacterId characterId = new(90000001);
        CorporationId corporationId = new(98000001);
        CharacterRecord character = CreateCharacter(characterId, "Kara Maken", isDefault: true, corporationId);

        await _harness.CharacterRepository.UpsertAsync(character);
        await _harness.TokenStore.WriteAsync(new EsiTokenRecord("access", "refresh", DateTimeOffset.UtcNow.AddHours(1), [
            "esi-assets.read_corporation_assets",
            "esi-industry.read_corporation_jobs",
            "esi-corporations.read_blueprints",
            "esi-corporations.read_corporation_membership"], Maybe<CharacterId>.Some(characterId)));
        await SeedAssetLookupDataAsync(_harness, [
            (34L, "Tritanium", "Mineral", "Material"),
            (35L, "Pyerite", "Mineral", "Material")]);
        await _harness.AssetRepository.ReplaceAsync(characterId.Value, [CreateStoredAsset(characterId.Value, 7100001, 34, "Tritanium")]);
        await _harness.OwnedBlueprintRepository.ReplaceAsync(characterId.Value, [CreateBlueprint(characterId.Value, 28607, "Character Blueprint")]);

        _harness.CapabilityResolver.ResolveAsync(corporationId, characterId, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CorporationCapabilityState>.Success(new CorporationCapabilityState(
                true, true, true, true, true, true, true, true, true))));
        _harness.EsiClient.GetNamesAsync(Arg.Any<IReadOnlyList<long>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<EsiEntityName>>.Success([
                new EsiEntityName(corporationId.Value, "corporation", "Acme Holdings")
            ])));
        _harness.CorporationAssetService.RefreshAsync(corporationId, characterId, Arg.Any<CancellationToken>())
            .Returns(_ => PersistCorporationAssetsAsync(_harness.AssetRepository, corporationId, [CreateAsset(corporationId.Value, 7200001, 35, "Pyerite") ]));
        _harness.CorporationBlueprintService.RefreshAsync(corporationId, characterId, Arg.Any<CancellationToken>())
            .Returns(_ => PersistCorporationBlueprintsAsync(_harness.OwnedBlueprintRepository, corporationId, [CreateBlueprint(corporationId.Value, 28608, "Corporation Blueprint") ]));
        _harness.CorporationIndustryJobService.RefreshAsync(corporationId, Arg.Any<CancellationToken>())
            .Returns(_ => PersistCorporationJobsAsync(_harness.IndustryJobRepository, characterId, corporationId, [CreateJob(99, characterId, IndustryJobScope.Corporation, 11)]));

        Result<CorporationConnectionRecord> result = await _harness.Service.ConnectCorporationAsync(characterId);
        AssetsQueryService assetsQueryService = new(_harness.AssetReadRepository, _harness.CharacterManagementQueryService);
        Result<IReadOnlyList<OwnedBlueprintViewRecord>> blueprintResult = await _harness.OwnedBlueprintViewRepository.GetByOwnersAsync([characterId.Value, corporationId.Value]);
        AssetsScreenData assetsScreenData = await assetsQueryService.GetScreenDataAsync();
        Result<IReadOnlyList<IndustryJobRecord>> jobsResult = await _harness.IndustryJobRepository.GetByInstallerIdAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        assetsScreenData.OwnerOptions.Select(option => option.DisplayName).Should().Contain(["Kara Maken", "Acme Holdings"]);
        assetsScreenData.Assets.Select(asset => asset.OwnerId).Should().BeEquivalentTo([characterId.Value, corporationId.Value]);
        blueprintResult.Value.Select(record => record.OwnerName).Should().BeEquivalentTo(["Kara Maken", "Acme Holdings"]);
        jobsResult.Value.Should().Contain(job => job.Scope == IndustryJobScope.Corporation && job.JobId == 99);
    }

    [Fact]
    public async Task RefreshCorporationAsync_WhenCapabilityIsLost_ClearsCorporationData()
    {
        CharacterId characterId = new(90000001);
        CorporationId corporationId = new(98000001);
        CharacterRecord character = CreateCharacter(characterId, "Kara Maken", isDefault: true, corporationId);

        await _harness.CharacterRepository.UpsertAsync(character);
        await _harness.TokenStore.WriteAsync(new EsiTokenRecord("access", "refresh", DateTimeOffset.UtcNow.AddHours(1), ["esi-assets.read_corporation_assets"], Maybe<CharacterId>.Some(characterId)));
        await _harness.CorporationConnectionRepository.UpsertAsync(new CorporationConnectionRecord(corporationId, "Acme Holdings", characterId, true, true, true, true, true));
        await _harness.AssetRepository.ReplaceAsync(corporationId.Value, [CreateStoredAsset(corporationId.Value, 7200001, 35, "Pyerite")]);
        await _harness.OwnedBlueprintRepository.ReplaceAsync(corporationId.Value, [CreateBlueprint(corporationId.Value, 28608, "Corporation Blueprint")]);
        await _harness.IndustryJobRepository.ReplaceAsync(characterId, IndustryJobScope.Corporation, [CreateJob(99, characterId, IndustryJobScope.Corporation, 11)]);

        _harness.CapabilityResolver.ResolveAsync(corporationId, characterId, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CorporationCapabilityState>.Success(new CorporationCapabilityState(
                false, false, false, false, false, false, false, false, false))));

        Result<CorporationConnectionRecord> result = await _harness.Service.RefreshCorporationAsync(corporationId);
        Maybe<CorporationConnectionRecord> connection = await _harness.CorporationConnectionRepository.GetByIdAsync(corporationId);
        Result<IReadOnlyList<StoredAssetRecord>> assets = await _harness.AssetRepository.GetByOwnerIdAsync(corporationId.Value);
        Result<IReadOnlyList<OwnedBlueprintRecord>> blueprints = await _harness.OwnedBlueprintRepository.GetByUserAsync(corporationId.Value);
        Result<IReadOnlyList<IndustryJobRecord>> jobs = await _harness.IndustryJobRepository.GetByInstallerIdAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        connection.HasValue.Should().BeTrue();
        connection.Value.HasAssetAccess.Should().BeFalse();
        connection.Value.HasIndustryJobAccess.Should().BeFalse();
        connection.Value.HasBlueprintAccess.Should().BeFalse();
        assets.Value.Should().BeEmpty();
        blueprints.Value.Should().BeEmpty();
        jobs.Value.Should().NotContain(job => job.Scope == IndustryJobScope.Corporation);
    }

    [Fact]
    public async Task DeleteAsync_WhenCharacterAuthorizesCorporation_CascadesPersonalAndCorporationData()
    {
        CharacterId characterId = new(90000001);
        CharacterId remainingCharacterId = new(90000002);
        CorporationId corporationId = new(98000001);

        await _harness.CharacterRepository.UpsertAsync(CreateCharacter(characterId, "Kara Maken", isDefault: true, corporationId));
        await _harness.CharacterRepository.UpsertAsync(CreateCharacter(remainingCharacterId, "Mina Kall", isDefault: false, new CorporationId(98000002)));
        await _harness.TokenStore.WriteAsync(new EsiTokenRecord("access", "refresh", DateTimeOffset.UtcNow.AddHours(1), [], Maybe<CharacterId>.Some(characterId)));
        await _harness.CorporationConnectionRepository.UpsertAsync(new CorporationConnectionRecord(corporationId, "Acme Holdings", characterId, true, true, true, true, true));
        await _harness.AssetRepository.ReplaceAsync(characterId.Value, [CreateStoredAsset(characterId.Value, 7100001, 34, "Tritanium")]);
        await _harness.AssetRepository.ReplaceAsync(corporationId.Value, [CreateStoredAsset(corporationId.Value, 7200001, 35, "Pyerite")]);
        await _harness.OwnedBlueprintRepository.ReplaceAsync(characterId.Value, [CreateBlueprint(characterId.Value, 28607, "Character Blueprint")]);
        await _harness.OwnedBlueprintRepository.ReplaceAsync(corporationId.Value, [CreateBlueprint(corporationId.Value, 28608, "Corporation Blueprint")]);
        await _harness.IndustryJobRepository.ReplaceAsync(characterId, IndustryJobScope.Personal, [CreateJob(42, characterId, IndustryJobScope.Personal, 1)]);
        await _harness.IndustryJobRepository.ReplaceAsync(characterId, IndustryJobScope.Corporation, [CreateJob(99, characterId, IndustryJobScope.Corporation, 11)]);

        Result<IReadOnlyList<CharacterRecord>> result = await _harness.Service.DeleteAsync(characterId);
        Result<IReadOnlyList<CharacterRecord>> characters = await _harness.CharacterRepository.GetAllAsync();
        Maybe<CorporationConnectionRecord> corporation = await _harness.CorporationConnectionRepository.GetByIdAsync(corporationId);
        Result<IReadOnlyList<StoredAssetRecord>> personalAssets = await _harness.AssetRepository.GetByOwnerIdAsync(characterId.Value);
        Result<IReadOnlyList<StoredAssetRecord>> corporationAssets = await _harness.AssetRepository.GetByOwnerIdAsync(corporationId.Value);
        Result<IReadOnlyList<OwnedBlueprintRecord>> personalBlueprints = await _harness.OwnedBlueprintRepository.GetByUserAsync(characterId.Value);
        Result<IReadOnlyList<OwnedBlueprintRecord>> corporationBlueprints = await _harness.OwnedBlueprintRepository.GetByUserAsync(corporationId.Value);
        Result<IReadOnlyList<IndustryJobRecord>> jobs = await _harness.IndustryJobRepository.GetByInstallerIdAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        characters.Value.Should().ContainSingle(record => record.CharacterId == remainingCharacterId && record.IsDefault);
        corporation.HasNoValue.Should().BeTrue();
        personalAssets.Value.Should().BeEmpty();
        corporationAssets.Value.Should().BeEmpty();
        personalBlueprints.Value.Should().BeEmpty();
        corporationBlueprints.Value.Should().BeEmpty();
        jobs.Value.Should().BeEmpty();
        _harness.TokenStore.GetStored(characterId).HasNoValue.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCorporationAsync_RemovesCorporationDataAndPreservesCharacterData()
    {
        CharacterId characterId = new(90000001);
        CorporationId corporationId = new(98000001);

        await _harness.CharacterRepository.UpsertAsync(CreateCharacter(characterId, "Kara Maken", isDefault: true, corporationId));
        await _harness.CorporationConnectionRepository.UpsertAsync(new CorporationConnectionRecord(corporationId, "Acme Holdings", characterId, true, true, true, true, true));
        await _harness.AssetRepository.ReplaceAsync(characterId.Value, [CreateStoredAsset(characterId.Value, 7100001, 34, "Tritanium")]);
        await _harness.AssetRepository.ReplaceAsync(corporationId.Value, [CreateStoredAsset(corporationId.Value, 7200001, 35, "Pyerite")]);
        await _harness.OwnedBlueprintRepository.ReplaceAsync(corporationId.Value, [CreateBlueprint(corporationId.Value, 28608, "Corporation Blueprint")]);
        await _harness.IndustryJobRepository.ReplaceAsync(characterId, IndustryJobScope.Personal, [CreateJob(42, characterId, IndustryJobScope.Personal, 1)]);
        await _harness.IndustryJobRepository.ReplaceAsync(characterId, IndustryJobScope.Corporation, [CreateJob(99, characterId, IndustryJobScope.Corporation, 11)]);

        Result<IReadOnlyList<CorporationConnectionRecord>> result = await _harness.Service.DeleteCorporationAsync(corporationId);
        Result<IReadOnlyList<CharacterRecord>> characters = await _harness.CharacterRepository.GetAllAsync();
        Result<IReadOnlyList<StoredAssetRecord>> personalAssets = await _harness.AssetRepository.GetByOwnerIdAsync(characterId.Value);
        Result<IReadOnlyList<StoredAssetRecord>> corporationAssets = await _harness.AssetRepository.GetByOwnerIdAsync(corporationId.Value);
        Result<IReadOnlyList<IndustryJobRecord>> jobs = await _harness.IndustryJobRepository.GetByInstallerIdAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        characters.Value.Should().ContainSingle(record => record.CharacterId == characterId);
        personalAssets.Value.Should().ContainSingle(asset => asset.OwnerId == characterId.Value);
        corporationAssets.Value.Should().BeEmpty();
        jobs.Value.Should().ContainSingle(job => job.Scope == IndustryJobScope.Personal && job.JobId == 42);
    }

    private static CharacterRecord CreateCharacter(CharacterId characterId, string name, bool isDefault, CorporationId? corporationId = null) =>
        new(characterId, name, corporationId ?? new CorporationId(98000001), Maybe<AllianceId>.None, isDefault);

    private static AssetRecord CreateAsset(long ownerId, long itemId, long typeId, string itemName) =>
        new(ownerId, itemId, 60003760L, new TypeId(typeId), 1, 4, true, false, itemName);

    private static StoredAssetRecord CreateStoredAsset(long ownerId, long itemId, long typeId, string itemName) =>
        new(ownerId, itemId, 60003760L, new TypeId(typeId), 1, 4, true, false, itemName);

    private static OwnedBlueprintRecord CreateBlueprint(long ownerId, long blueprintId, string blueprintName) =>
        new(ownerId, new ItemId(7000000 + blueprintId), 60003760L, new BlueprintId(blueprintId), blueprintName, 1, 10, 20, -1, 1, true, true);

    private static IndustryJobRecord CreateJob(long jobId, CharacterId installerId, IndustryJobScope scope, int activityId) => new(
        jobId,
        installerId,
        60003760L,
        60003760L,
        activityId,
        80000000L + jobId,
        new TypeId(28607),
        70000001L,
        70000002L,
        1,
        1000,
        1,
        1.0,
        Maybe<TypeId>.Some(new TypeId(34)),
        "active",
        3600,
        DateTimeOffset.UtcNow.AddHours(-1),
        DateTimeOffset.UtcNow.AddHours(1),
        null,
        null,
        Maybe<CharacterId>.None,
        0,
        scope);

    private static Task<Result<CharacterSnapshot>> PersistCharacterSnapshotAsync(ICharacterRepository repository, CharacterRecord character) =>
        PersistCharacterSnapshotCoreAsync(repository, character);

    private static async Task<Result<CharacterSnapshot>> PersistCharacterSnapshotCoreAsync(ICharacterRepository repository, CharacterRecord character)
    {
        Result<CharacterRecord> upsertResult = await repository.UpsertAsync(character);
        return upsertResult.IsFailure
            ? Result<CharacterSnapshot>.Failure(upsertResult.Error)
            : Result<CharacterSnapshot>.Success(new CharacterSnapshot(character, [], []));
    }

    private static Task<Result<IReadOnlyList<AssetRecord>>> PersistCharacterAssetsAsync(IAssetRepository repository, CharacterId characterId, IReadOnlyList<AssetRecord> assets) =>
        PersistAssetsAsync(repository, characterId.Value, assets);

    private static Task<Result<IReadOnlyList<AssetRecord>>> PersistCorporationAssetsAsync(IAssetRepository repository, CorporationId corporationId, IReadOnlyList<AssetRecord> assets) =>
        PersistAssetsAsync(repository, corporationId.Value, assets);

    private static async Task<Result<IReadOnlyList<AssetRecord>>> PersistAssetsAsync(IAssetRepository repository, long ownerId, IReadOnlyList<AssetRecord> assets)
    {
        Result<IReadOnlyList<StoredAssetRecord>> replaceResult = await repository.ReplaceAsync(ownerId, assets.Select(asset => new StoredAssetRecord(asset.OwnerId, asset.ItemId, asset.LocationId, asset.TypeId, asset.Quantity, asset.FlagId, asset.IsSingleton, asset.IsBlueprintCopy, asset.ItemName)).ToArray());
        return replaceResult.IsFailure
            ? Result<IReadOnlyList<AssetRecord>>.Failure(replaceResult.Error)
            : Result<IReadOnlyList<AssetRecord>>.Success(assets);
    }

    private static async Task<Result<IndustryJobSnapshot>> PersistCharacterJobsAsync(IIndustryJobRepository repository, CharacterId characterId, IReadOnlyList<IndustryJobRecord> jobs)
    {
        Result<IReadOnlyList<IndustryJobRecord>> replaceResult = await repository.ReplaceAsync(characterId, IndustryJobScope.Personal, jobs);
        return replaceResult.IsFailure
            ? Result<IndustryJobSnapshot>.Failure(replaceResult.Error)
            : Result<IndustryJobSnapshot>.Success(new IndustryJobSnapshot(characterId, jobs.Select(job => new IndustryJob(job.JobId, job.InstallerId.Value, job.ActivityId, job.Status, job.StartDate, job.EndDate)).ToArray(), new IndustryJobSummary(jobs.Count(job => job.ActivityId == 1), 0, jobs.Count(job => job.ActivityId == 11), jobs.Count, jobs.Count, 0)));
    }

    private static async Task<Result<CorporationIndustryJobSnapshot>> PersistCorporationJobsAsync(IIndustryJobRepository repository, CharacterId installerId, CorporationId corporationId, IReadOnlyList<IndustryJobRecord> jobs)
    {
        Result<IReadOnlyList<IndustryJobRecord>> replaceResult = await repository.ReplaceAsync(installerId, IndustryJobScope.Corporation, jobs);
        return replaceResult.IsFailure
            ? Result<CorporationIndustryJobSnapshot>.Failure(replaceResult.Error)
            : Result<CorporationIndustryJobSnapshot>.Success(new CorporationIndustryJobSnapshot(corporationId, jobs.Select(job => new IndustryJob(job.JobId, job.InstallerId.Value, job.ActivityId, job.Status, job.StartDate, job.EndDate)).ToArray(), new IndustryJobSummary(jobs.Count(job => job.ActivityId == 1), 0, jobs.Count(job => job.ActivityId == 11), jobs.Count, jobs.Count, 0)));
    }

    private static async Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> PersistCorporationBlueprintsAsync(IOwnedBlueprintRepository repository, CorporationId corporationId, IReadOnlyList<OwnedBlueprintRecord> blueprints)
    {
        Result<IReadOnlyList<OwnedBlueprintRecord>> replaceResult = await repository.ReplaceAsync(corporationId.Value, blueprints);
        return replaceResult.IsFailure
            ? Result<IReadOnlyList<OwnedBlueprintRecord>>.Failure(replaceResult.Error)
            : Result<IReadOnlyList<OwnedBlueprintRecord>>.Success(blueprints);
    }

    private static async Task SeedAssetLookupDataAsync(ServiceHarness harness, IReadOnlyList<(long TypeId, string TypeName, string GroupName, string CategoryName)> itemTypes)
    {
        using System.Data.IDbConnection connection = harness.Fixture.ConnectionFactory.CreateConnection();
        await connection.ExecuteAsync("INSERT INTO INVENTORY_FLAGS (FlagID, FlagText, container, sort_order) VALUES (4, 'Hangar', 0, 10)");
        await connection.ExecuteAsync("INSERT INTO REGIONS (regionID, regionName) VALUES (10000002, 'The Forge')");
        await connection.ExecuteAsync("INSERT INTO SOLAR_SYSTEMS (solarSystemID, solarSystemName, regionID, SECURITY) VALUES (30000142, 'Jita', 10000002, 0.9)");
        await connection.ExecuteAsync("INSERT INTO STATIONS (STATION_ID, STATION_NAME, SOLAR_SYSTEM_ID, regionID) VALUES (60003760, 'Jita IV - Moon 4 - Caldari Navy Assembly Plant', 30000142, 10000002)");
        await connection.ExecuteAsync("INSERT INTO ITEM_LOOKUP (typeID, typeName, groupName, categoryName) VALUES (@TypeId, @TypeName, @GroupName, @CategoryName)",
            itemTypes.Select(item => new { item.TypeId, item.TypeName, item.GroupName, item.CategoryName }));
    }

    private sealed class ServiceHarness : IDisposable
    {
        public InMemoryDbFixture Fixture { get; } = new();
        public ICharacterRepository CharacterRepository { get; }
        public ICorporationConnectionRepository CorporationConnectionRepository { get; }
        public IAssetRepository AssetRepository { get; }
        public IAssetReadRepository AssetReadRepository { get; }
        public IIndustryJobRepository IndustryJobRepository { get; }
        public IOwnedBlueprintRepository OwnedBlueprintRepository { get; }
        public IOwnedBlueprintViewRepository OwnedBlueprintViewRepository { get; }
        public InMemoryTokenStore TokenStore { get; } = new();
        public ICharacterManagementQueryService CharacterManagementQueryService { get; }
        public CharacterManagementService Service { get; }
        public ICharacterAssetService CharacterAssetService { get; } = Substitute.For<ICharacterAssetService>();
        public ICorporationAssetService CorporationAssetService { get; } = Substitute.For<ICorporationAssetService>();
        public ICorporationBlueprintService CorporationBlueprintService { get; } = Substitute.For<ICorporationBlueprintService>();
        public ICorporationCapabilityResolver CapabilityResolver { get; } = Substitute.For<ICorporationCapabilityResolver>();
        public ICharacterService CharacterService { get; } = Substitute.For<ICharacterService>();
        public ICharacterIndustryJobService CharacterIndustryJobService { get; } = Substitute.For<ICharacterIndustryJobService>();
        public ICorporationIndustryJobService CorporationIndustryJobService { get; } = Substitute.For<ICorporationIndustryJobService>();
        public IEsiClient EsiClient { get; } = Substitute.For<IEsiClient>();
        public IEsiInteractiveLoginService InteractiveLoginService { get; } = Substitute.For<IEsiInteractiveLoginService>();
        public IResearchAgentService ResearchAgentService { get; } = Substitute.For<IResearchAgentService>();

        public ServiceHarness()
        {
            CharacterRepository = new SqliteCharacterRepository(Fixture.ConnectionFactory);
            CorporationConnectionRepository = new SqliteCorporationConnectionRepository(Fixture.ConnectionFactory);
            AssetRepository = new SqliteAssetRepository(Fixture.ConnectionFactory);
            AssetReadRepository = new SqliteAssetReadRepository(Fixture.ConnectionFactory);
            IndustryJobRepository = new SqliteIndustryJobRepository(Fixture.ConnectionFactory);
            OwnedBlueprintRepository = new SqliteOwnedBlueprintRepository(Fixture.ConnectionFactory);
            OwnedBlueprintViewRepository = new SqliteOwnedBlueprintReadRepository(Fixture.ConnectionFactory);
            CharacterManagementQueryService = new CharacterManagementQueryService(CharacterRepository, CorporationConnectionRepository, TokenStore);

            Service = new CharacterManagementService(
                CharacterRepository,
                AssetRepository,
                CharacterAssetService,
                CorporationAssetService,
                CorporationBlueprintService,
                CapabilityResolver,
                CorporationConnectionRepository,
                CharacterService,
                CharacterIndustryJobService,
                CorporationIndustryJobService,
                IndustryJobRepository,
                OwnedBlueprintRepository,
                EsiClient,
                InteractiveLoginService,
                ResearchAgentService,
                TokenStore);
        }

        public void Dispose() => Fixture.Dispose();
    }

    private sealed class InMemoryTokenStore : IEsiTokenStore
    {
        private readonly Dictionary<long, EsiTokenRecord> _tokens = [];

        public Maybe<EsiTokenRecord> GetStored(CharacterId characterId) =>
            _tokens.TryGetValue(characterId.Value, out EsiTokenRecord? token)
                ? Maybe<EsiTokenRecord>.Some(token)
                : Maybe<EsiTokenRecord>.None;

        public Task<Maybe<EsiTokenRecord>> ReadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_tokens.Values.FirstOrDefault() is EsiTokenRecord token ? Maybe<EsiTokenRecord>.Some(token) : Maybe<EsiTokenRecord>.None);

        public Task<Maybe<EsiTokenRecord>> ReadAsync(CharacterId characterId, CancellationToken cancellationToken = default) =>
            Task.FromResult(GetStored(characterId));

        public Task<Result<EsiTokenRecord>> WriteAsync(EsiTokenRecord token, CancellationToken cancellationToken = default)
        {
            if (token.CharacterId.HasValue)
            {
                _tokens[token.CharacterId.Value.Value] = token;
            }

            return Task.FromResult(Result<EsiTokenRecord>.Success(token));
        }

        public Task<Result<bool>> ClearAsync(CancellationToken cancellationToken = default)
        {
            _tokens.Clear();
            return Task.FromResult(Result<bool>.Success(true));
        }

        public Task<Result<bool>> ClearAsync(CharacterId characterId, CancellationToken cancellationToken = default)
        {
            bool removed = _tokens.Remove(characterId.Value);
            return Task.FromResult(Result<bool>.Success(removed));
        }
    }
}