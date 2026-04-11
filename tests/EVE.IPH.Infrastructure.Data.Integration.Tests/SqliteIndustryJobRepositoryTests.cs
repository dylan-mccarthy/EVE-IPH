using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteIndustryJobRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly IIndustryJobRepository _sut;

    public SqliteIndustryJobRepositoryTests()
    {
        _sut = new SqliteIndustryJobRepository(_fixture.ConnectionFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ReplaceAsync_AndGetByInstallerIdAsync_PersistIndustryJobs()
    {
        CharacterId installerId = new(100_000_601);

        await _sut.ReplaceAsync(installerId, IndustryJobScope.Personal,
        [
            CreateJob(42, installerId, IndustryJobScope.Personal, 1),
        ]);

        var result = await _sut.GetByInstallerIdAsync(installerId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].JobId.Should().Be(42);
        result.Value[0].Scope.Should().Be(IndustryJobScope.Personal);
    }

    [Fact]
    public async Task ReplaceAsync_ReplacesOnlyMatchingScope()
    {
        CharacterId installerId = new(100_000_602);

        await _sut.ReplaceAsync(installerId, IndustryJobScope.Personal,
        [
            CreateJob(1, installerId, IndustryJobScope.Personal, 1),
        ]);
        await _sut.ReplaceAsync(installerId, IndustryJobScope.Corporation,
        [
            CreateJob(2, installerId, IndustryJobScope.Corporation, 11),
        ]);
        await _sut.ReplaceAsync(installerId, IndustryJobScope.Personal,
        [
            CreateJob(3, installerId, IndustryJobScope.Personal, 8),
        ]);

        var result = await _sut.GetByInstallerIdAsync(installerId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(job => job.JobId == 2 && job.Scope == IndustryJobScope.Corporation);
        result.Value.Should().Contain(job => job.JobId == 3 && job.Scope == IndustryJobScope.Personal);
    }

    private static IndustryJobRecord CreateJob(long jobId, CharacterId installerId, IndustryJobScope scope, int activityId) => new(
        jobId,
        installerId,
        60015068,
        60015068,
        activityId,
        9000 + jobId,
        new TypeId(28607),
        60015068,
        60015068,
        2,
        1_550_000.5,
        10,
        1.0,
        Maybe<TypeId>.Some(new TypeId(19720)),
        "active",
        3600,
        new DateTimeOffset(2026, 4, 10, 10, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 4, 10, 11, 0, 0, TimeSpan.Zero),
        null,
        null,
        Maybe<CharacterId>.None,
        0,
        scope);
}