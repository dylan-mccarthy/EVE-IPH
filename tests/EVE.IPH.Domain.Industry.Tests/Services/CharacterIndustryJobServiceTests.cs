using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Industry.Services;
using NSubstitute;

namespace EVE.IPH.Domain.Industry.Tests.Services;

public sealed class CharacterIndustryJobServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAsync_LoadsStoredJobsAndBuildsSummary()
    {
        CharacterId characterId = new(90000001);
        IIndustryJobRepository repository = Substitute.For<IIndustryJobRepository>();
        IIndustryJobDataSource dataSource = Substitute.For<IIndustryJobDataSource>();
        IIndustryJobService summaryService = new IndustryJobService();
        FakeTimeProvider timeProvider = new(Now);

        repository.GetByInstallerIdAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<IndustryJobRecord>>.Success([
                CreateRecord(characterId, 1, "active", Now.AddHours(-1), Now.AddHours(1)),
                CreateRecord(characterId, 8, "active", Now.AddHours(1), Now.AddHours(2)),
            ]));

        CharacterIndustryJobService service = new(repository, dataSource, summaryService, timeProvider);

        Result<Models.IndustryJobSnapshot> result = await service.GetAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Jobs.Should().HaveCount(2);
        result.Value.Summary.CurrentManufacturingJobs.Should().Be(1);
        result.Value.Summary.CurrentResearchJobs.Should().Be(1);
        result.Value.Summary.PendingJobs.Should().Be(1);
        result.Value.Summary.InProgressJobs.Should().Be(1);
    }

    [Fact]
    public async Task RefreshAsync_NormalizesReactionJobsAndPersistsResults()
    {
        CharacterId characterId = new(90000002);
        IIndustryJobRepository repository = Substitute.For<IIndustryJobRepository>();
        IIndustryJobDataSource dataSource = Substitute.For<IIndustryJobDataSource>();
        IIndustryJobService summaryService = new IndustryJobService();
        FakeTimeProvider timeProvider = new(Now);

        dataSource.GetCharacterJobsAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<IndustryJobData>>.Success([
                CreateData(characterId, 9, "active", Now.AddHours(-2), Now.AddHours(1)),
            ]));
        repository.ReplaceAsync(Arg.Any<CharacterId>(), Arg.Any<IReadOnlyList<IndustryJobRecord>>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<IReadOnlyList<IndustryJobRecord>>.Success(call.Arg<IReadOnlyList<IndustryJobRecord>>()));

        CharacterIndustryJobService service = new(repository, dataSource, summaryService, timeProvider);

        Result<Models.IndustryJobSnapshot> result = await service.RefreshAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.CurrentReactionJobs.Should().Be(1);
        result.Value.Jobs.Should().ContainSingle(job => job.ActivityId == 11);

        await repository.Received(1).ReplaceAsync(
            Arg.Is<CharacterId>(id => id == characterId),
            Arg.Is<IReadOnlyList<IndustryJobRecord>>(jobs => jobs.Count == 1 && jobs[0].ActivityId == 11 && jobs[0].InstallerId == characterId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_DataSourceFailure_ReturnsFailure()
    {
        CharacterId characterId = new(90000003);
        IIndustryJobRepository repository = Substitute.For<IIndustryJobRepository>();
        IIndustryJobDataSource dataSource = Substitute.For<IIndustryJobDataSource>();
        IIndustryJobService summaryService = new IndustryJobService();
        FakeTimeProvider timeProvider = new(Now);

        dataSource.GetCharacterJobsAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<IndustryJobData>>.Failure("industry.fetch.failed", "Unable to fetch jobs."));

        CharacterIndustryJobService service = new(repository, dataSource, summaryService, timeProvider);

        Result<Models.IndustryJobSnapshot> result = await service.RefreshAsync(characterId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("industry.fetch.failed");
        await repository.DidNotReceiveWithAnyArgs().ReplaceAsync(default, default!, default);
    }

    private static IndustryJobRecord CreateRecord(
        CharacterId characterId,
        int activityId,
        string status,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate)
    {
        return new IndustryJobRecord(
            1001,
            characterId,
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
            IndustryJobScope.Personal);
    }

    private static IndustryJobData CreateData(
        CharacterId characterId,
        int activityId,
        string status,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate)
    {
        return new IndustryJobData(
            1001,
            characterId,
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
            IndustryJobScope.Personal);
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}