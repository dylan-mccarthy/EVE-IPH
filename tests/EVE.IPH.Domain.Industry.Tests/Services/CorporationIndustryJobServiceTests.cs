using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Industry.Services;
using NSubstitute;

namespace EVE.IPH.Domain.Industry.Tests.Services;

public sealed class CorporationIndustryJobServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAsync_LoadsStoredCorporationJobsForKnownInstallers()
    {
        CorporationId corporationId = new(98000001);
        CharacterId firstInstaller = new(90000001);
        CharacterId secondInstaller = new(90000002);
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        IIndustryJobRepository industryJobRepository = Substitute.For<IIndustryJobRepository>();
        IIndustryJobDataSource industryJobDataSource = Substitute.For<IIndustryJobDataSource>();
        IIndustryJobService industryJobService = new IndustryJobService();
        FakeTimeProvider timeProvider = new(Now);

        characterRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CharacterRecord>>.Success([
                CreateCharacter(firstInstaller, corporationId),
                CreateCharacter(secondInstaller, corporationId),
                CreateCharacter(new CharacterId(90000003), new CorporationId(98000002)),
            ]));
        industryJobRepository.GetByInstallerIdAsync(firstInstaller, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<IndustryJobRecord>>.Success([
                CreateRecord(firstInstaller, 1, "active", Now.AddHours(-1), Now.AddHours(1), IndustryJobScope.Corporation),
                CreateRecord(firstInstaller, 8, "active", Now.AddHours(1), Now.AddHours(2), IndustryJobScope.Personal),
            ]));
        industryJobRepository.GetByInstallerIdAsync(secondInstaller, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<IndustryJobRecord>>.Success([
                CreateRecord(secondInstaller, 8, "active", Now.AddHours(1), Now.AddHours(2), IndustryJobScope.Corporation),
            ]));

        CorporationIndustryJobService service = new(characterRepository, industryJobRepository, industryJobDataSource, industryJobService, timeProvider);

        Result<Models.CorporationIndustryJobSnapshot> result = await service.GetAsync(corporationId);

        result.IsSuccess.Should().BeTrue();
        result.Value.CorporationId.Should().Be(corporationId);
        result.Value.Jobs.Should().HaveCount(2);
        result.Value.Jobs.Should().OnlyContain(job => job.InstallerId == firstInstaller.Value || job.InstallerId == secondInstaller.Value);
        result.Value.Summary.CurrentManufacturingJobs.Should().Be(1);
        result.Value.Summary.CurrentResearchJobs.Should().Be(1);
    }

    [Fact]
    public async Task RefreshAsync_FiltersUnknownInstallersAndNormalizesReactionJobs()
    {
        CorporationId corporationId = new(98000001);
        CharacterId firstInstaller = new(90000001);
        CharacterId secondInstaller = new(90000002);
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        IIndustryJobRepository industryJobRepository = Substitute.For<IIndustryJobRepository>();
        IIndustryJobDataSource industryJobDataSource = Substitute.For<IIndustryJobDataSource>();
        IIndustryJobService industryJobService = new IndustryJobService();
        FakeTimeProvider timeProvider = new(Now);

        characterRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CharacterRecord>>.Success([
                CreateCharacter(firstInstaller, corporationId),
                CreateCharacter(secondInstaller, corporationId),
            ]));
        industryJobDataSource.GetCorporationJobsAsync(corporationId, firstInstaller, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<IndustryJobData>>.Success([
                CreateData(firstInstaller, 9, "active", Now.AddHours(-2), Now.AddHours(1), IndustryJobScope.Corporation),
                CreateData(new CharacterId(90000099), 1, "active", Now.AddHours(-1), Now.AddHours(3), IndustryJobScope.Corporation),
            ]));
        industryJobRepository.ReplaceAsync(Arg.Any<CharacterId>(), Arg.Any<IndustryJobScope>(), Arg.Any<IReadOnlyList<IndustryJobRecord>>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<IReadOnlyList<IndustryJobRecord>>.Success(call.ArgAt<IReadOnlyList<IndustryJobRecord>>(2)));

        CorporationIndustryJobService service = new(characterRepository, industryJobRepository, industryJobDataSource, industryJobService, timeProvider);

        Result<Models.CorporationIndustryJobSnapshot> result = await service.RefreshAsync(corporationId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Jobs.Should().ContainSingle();
        result.Value.Jobs.Should().ContainSingle(job => job.InstallerId == firstInstaller.Value && job.ActivityId == 11);
        result.Value.Summary.CurrentReactionJobs.Should().Be(1);

        await industryJobRepository.Received(1).ReplaceAsync(
            Arg.Is<CharacterId>(id => id == firstInstaller),
            Arg.Is<IndustryJobScope>(scope => scope == IndustryJobScope.Corporation),
            Arg.Is<IReadOnlyList<IndustryJobRecord>>(jobs => jobs.Count == 1 && jobs[0].ActivityId == 11 && jobs[0].Scope == IndustryJobScope.Corporation),
            Arg.Any<CancellationToken>());
        await industryJobRepository.Received(1).ReplaceAsync(
            Arg.Is<CharacterId>(id => id == secondInstaller),
            Arg.Is<IndustryJobScope>(scope => scope == IndustryJobScope.Corporation),
            Arg.Is<IReadOnlyList<IndustryJobRecord>>(jobs => jobs.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_CharacterRepositoryFailure_ReturnsFailure()
    {
        CorporationId corporationId = new(98000001);
        ICharacterRepository characterRepository = Substitute.For<ICharacterRepository>();
        IIndustryJobRepository industryJobRepository = Substitute.For<IIndustryJobRepository>();
        IIndustryJobDataSource industryJobDataSource = Substitute.For<IIndustryJobDataSource>();
        IIndustryJobService industryJobService = new IndustryJobService();
        FakeTimeProvider timeProvider = new(Now);

        characterRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CharacterRecord>>.Failure("characters.load.failed", "Unable to load characters."));

        CorporationIndustryJobService service = new(characterRepository, industryJobRepository, industryJobDataSource, industryJobService, timeProvider);

        Result<Models.CorporationIndustryJobSnapshot> result = await service.RefreshAsync(corporationId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("characters.load.failed");
        await industryJobDataSource.DidNotReceiveWithAnyArgs().GetCorporationJobsAsync(default, default, default);
    }

    private static CharacterRecord CreateCharacter(CharacterId characterId, CorporationId corporationId)
    {
        return new CharacterRecord(characterId, $"Character {characterId.Value}", corporationId, Maybe<AllianceId>.None, false);
    }

    private static IndustryJobRecord CreateRecord(
        CharacterId installerId,
        int activityId,
        string status,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        IndustryJobScope scope)
    {
        return new IndustryJobRecord(
            1001 + installerId.Value,
            installerId,
            6001,
            7001,
            activityId,
            8001,
            new TypeId(9001),
            10001,
            11001,
            2,
            123.45,
            0,
            0,
            Maybe<TypeId>.None,
            status,
            3600,
            startDate,
            endDate,
            null,
            null,
            Maybe<CharacterId>.None,
            0,
            scope);
    }

    private static IndustryJobData CreateData(
        CharacterId installerId,
        int activityId,
        string status,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        IndustryJobScope scope)
    {
        return new IndustryJobData(
            1001 + installerId.Value,
            installerId,
            6001,
            7001,
            activityId,
            8001,
            new TypeId(9001),
            10001,
            11001,
            2,
            123.45,
            0,
            0,
            Maybe<TypeId>.None,
            status,
            3600,
            startDate,
            endDate,
            null,
            null,
            Maybe<CharacterId>.None,
            0,
            scope);
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}