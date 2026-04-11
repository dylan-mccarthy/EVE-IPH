using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteMarketCacheRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly IMarketCacheRepository _sut;

    public SqliteMarketCacheRepositoryTests()
    {
        _sut = new SqliteMarketCacheRepository(_fixture.ConnectionFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task UpsertAsync_NewRecord_InsertsAndReturnsRecord()
    {
        MarketCacheRecord record = BuildRecord(typeId: 34, regionOrSystem: 10_000_002, priceSource: 1);

        Result<MarketCacheRecord> result = await _sut.UpsertAsync(record);

        result.IsSuccess.Should().BeTrue();
        result.Value.TypeId.Value.Should().Be(34);
        result.Value.SellAvg.Should().BeApproximately(500.0, 0.001);
    }

    [Fact]
    public async Task GetAsync_AfterInsert_ReturnsRecord()
    {
        long typeId = 35;
        long region = 10_000_002;
        int source = 1;

        await _sut.UpsertAsync(BuildRecord(typeId, region, source));

        Maybe<MarketCacheRecord> result = await _sut.GetAsync(new TypeId(typeId), region, source);

        result.HasValue.Should().BeTrue();
        result.Value.BuyMax.Should().BeApproximately(100.0, 0.001);
    }

    [Fact]
    public async Task GetAsync_MissingRecord_ReturnsNone()
    {
        Maybe<MarketCacheRecord> result = await _sut.GetAsync(new TypeId(999_999), 1, 1);

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task UpsertAsync_ExistingRecord_UpdatesPrices()
    {
        long typeId = 36;
        long region = 10_000_002;
        int source = 1;

        await _sut.UpsertAsync(BuildRecord(typeId, region, source, sellAvg: 100.0));
        await _sut.UpsertAsync(BuildRecord(typeId, region, source, sellAvg: 200.0));

        Maybe<MarketCacheRecord> result = await _sut.GetAsync(new TypeId(typeId), region, source);

        result.HasValue.Should().BeTrue();
        result.Value.SellAvg.Should().BeApproximately(200.0, 0.001);
    }

    [Fact]
    public async Task DeleteAsync_ExistingRecord_ReturnsTrue()
    {
        long typeId = 37;
        long region = 10_000_002;
        int source = 1;

        await _sut.UpsertAsync(BuildRecord(typeId, region, source));

        Result<bool> result = await _sut.DeleteAsync(new TypeId(typeId), region, source);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        (await _sut.GetAsync(new TypeId(typeId), region, source)).HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAllAsync_RemovesAllRecords()
    {
        await _sut.UpsertAsync(BuildRecord(38, 10_000_002, 1));
        await _sut.UpsertAsync(BuildRecord(39, 10_000_002, 1));

        Result<bool> result = await _sut.DeleteAllAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        (await _sut.GetAsync(new TypeId(38), 10_000_002, 1)).HasValue.Should().BeFalse();
        (await _sut.GetAsync(new TypeId(39), 10_000_002, 1)).HasValue.Should().BeFalse();
    }

    private static MarketCacheRecord BuildRecord(long typeId, long regionOrSystem, int priceSource, double sellAvg = 500.0) =>
        new(new TypeId(typeId),
            BuyVolume: 1000, BuyAvg: 90.0, BuyWeightedAvg: 95.0, BuyMax: 100.0, BuyMin: 80.0,
            SellVolume: 2000, SellAvg: sellAvg, SellWeightedAvg: 510.0, SellMax: 600.0, SellMin: 400.0,
            regionOrSystem, DateTime.UtcNow, priceSource);
}
