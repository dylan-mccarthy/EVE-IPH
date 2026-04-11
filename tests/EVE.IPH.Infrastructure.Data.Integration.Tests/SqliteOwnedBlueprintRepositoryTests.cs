using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteOwnedBlueprintRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly IOwnedBlueprintRepository _sut;

    public SqliteOwnedBlueprintRepositoryTests()
    {
        _sut = new SqliteOwnedBlueprintRepository(_fixture.ConnectionFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task UpsertAsync_NewRecord_InsertsAndReturnsRecord()
    {
        OwnedBlueprintRecord record = BuildRecord(userId: 1, blueprintId: 1001, me: 10, te: 20);

        Result<OwnedBlueprintRecord> result = await _sut.UpsertAsync(record);

        result.IsSuccess.Should().BeTrue();
        result.Value.BlueprintId.Value.Should().Be(1001);
        result.Value.Me.Should().Be(10);
    }

    [Fact]
    public async Task GetByUserAsync_AfterInsert_ReturnsBlueprintsForUser()
    {
        long userId = 2;
        await _sut.UpsertAsync(BuildRecord(userId, 2001));
        await _sut.UpsertAsync(BuildRecord(userId, 2002));
        await _sut.UpsertAsync(BuildRecord(userId: 99, 9999));

        Result<IReadOnlyList<OwnedBlueprintRecord>> result = await _sut.GetByUserAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().AllSatisfy(r => r.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task UpsertAsync_ExistingRecord_UpdatesMeAndTe()
    {
        long userId = 3;
        BlueprintId bpId = new(3001);

        await _sut.UpsertAsync(BuildRecord(userId, bpId.Value, me: 0, te: 0));
        await _sut.UpsertAsync(BuildRecord(userId, bpId.Value, me: 10, te: 20));

        Result<IReadOnlyList<OwnedBlueprintRecord>> result = await _sut.GetByUserAsync(userId);

        result.IsSuccess.Should().BeTrue();
        OwnedBlueprintRecord updated = result.Value.Single();
        updated.Me.Should().Be(10);
        updated.Te.Should().Be(20);
    }

    [Fact]
    public async Task DeleteAsync_ExistingRecord_ReturnsTrue()
    {
        long userId = 4;
        BlueprintId bpId = new(4001);
        await _sut.UpsertAsync(BuildRecord(userId, bpId.Value));

        Result<bool> result = await _sut.DeleteAsync(userId, bpId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_MissingRecord_ReturnsFalse()
    {
        Result<bool> result = await _sut.DeleteAsync(999, new BlueprintId(999_999));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    private static OwnedBlueprintRecord BuildRecord(long userId, long blueprintId, int me = 0, int te = 0) =>
        new(userId, new ItemId(blueprintId + 10_000), LocationId: 0, new BlueprintId(blueprintId),
            $"Blueprint {blueprintId}", Quantity: 1, me, te, Runs: -1, BpType: 1, Owned: true, Scanned: false);
}
