using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

/// <summary>SQLite-backed implementation of <see cref="IMarketCacheRepository"/>.</summary>
public sealed class SqliteMarketCacheRepository : IMarketCacheRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteMarketCacheRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Maybe<MarketCacheRecord>> GetAsync(TypeId typeId, long regionOrSystem, int priceSource, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT typeID, buyVolume, buyAvg, buyweightedAvg, buyMax, buyMin,
                       sellVolume, sellAvg, sellweightedAvg, sellMax, sellMin,
                       RegionOrSystem, UpdateDate, PRICE_SOURCE
                FROM ITEM_PRICES_CACHE
                WHERE typeID = @TypeId AND RegionOrSystem = @RegionOrSystem AND PRICE_SOURCE = @PriceSource
                """;

            MarketCacheDto? row = await connection.QueryFirstOrDefaultAsync<MarketCacheDto>(
                new CommandDefinition(sql, new { TypeId = typeId.Value, RegionOrSystem = regionOrSystem, PriceSource = priceSource }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return row is null ? Maybe<MarketCacheRecord>.None : Maybe<MarketCacheRecord>.Some(MapRecord(row));
        }
        catch (Exception)
        {
            return Maybe<MarketCacheRecord>.None;
        }
    }

    public async Task<Result<MarketCacheRecord>> UpsertAsync(MarketCacheRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                INSERT INTO ITEM_PRICES_CACHE
                    (typeID, buyVolume, buyAvg, buyweightedAvg, buyMax, buyMin,
                     sellVolume, sellAvg, sellweightedAvg, sellMax, sellMin,
                     RegionOrSystem, UpdateDate, PRICE_SOURCE)
                VALUES
                    (@TypeId, @BuyVolume, @BuyAvg, @BuyWeightedAvg, @BuyMax, @BuyMin,
                     @SellVolume, @SellAvg, @SellWeightedAvg, @SellMax, @SellMin,
                     @RegionOrSystem, @UpdateDate, @PriceSource)
                ON CONFLICT(typeID, RegionOrSystem, PRICE_SOURCE) DO UPDATE SET
                    buyVolume = excluded.buyVolume,
                    buyAvg = excluded.buyAvg,
                    buyweightedAvg = excluded.buyweightedAvg,
                    buyMax = excluded.buyMax,
                    buyMin = excluded.buyMin,
                    sellVolume = excluded.sellVolume,
                    sellAvg = excluded.sellAvg,
                    sellweightedAvg = excluded.sellweightedAvg,
                    sellMax = excluded.sellMax,
                    sellMin = excluded.sellMin,
                    UpdateDate = excluded.UpdateDate
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(sql, ToParam(record), cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return Result<MarketCacheRecord>.Success(record);
        }
        catch (Exception ex)
        {
            return Result<MarketCacheRecord>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<bool>> DeleteAsync(TypeId typeId, long regionOrSystem, int priceSource, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "DELETE FROM ITEM_PRICES_CACHE WHERE typeID = @TypeId AND RegionOrSystem = @RegionOrSystem AND PRICE_SOURCE = @PriceSource";

            int affected = await connection.ExecuteAsync(
                new CommandDefinition(sql, new { TypeId = typeId.Value, RegionOrSystem = regionOrSystem, PriceSource = priceSource }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return Result<bool>.Success(affected > 0);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<bool>> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                new CommandDefinition("DELETE FROM ITEM_PRICES_CACHE", cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("DB_ERROR", ex.Message);
        }
    }

    private static object ToParam(MarketCacheRecord r) => new
    {
        TypeId = r.TypeId.Value,
        r.BuyVolume,
        r.BuyAvg,
        r.BuyWeightedAvg,
        r.BuyMax,
        r.BuyMin,
        r.SellVolume,
        r.SellAvg,
        r.SellWeightedAvg,
        r.SellMax,
        r.SellMin,
        r.RegionOrSystem,
        UpdateDate = r.UpdateDate.ToString("O"),
        PriceSource = r.PriceSource,
    };

    private static MarketCacheRecord MapRecord(MarketCacheDto row) => new(
        new TypeId(row.typeID),
        row.buyVolume,
        row.buyAvg,
        row.buyweightedAvg,
        row.buyMax,
        row.buyMin,
        row.sellVolume,
        row.sellAvg,
        row.sellweightedAvg,
        row.sellMax,
        row.sellMin,
        row.RegionOrSystem,
        DateTime.Parse(row.UpdateDate, null, System.Globalization.DateTimeStyles.RoundtripKind),
        row.PRICE_SOURCE);

    private sealed class MarketCacheDto
    {
        public long typeID { get; init; }
        public double buyVolume { get; init; }
        public double buyAvg { get; init; }
        public double buyweightedAvg { get; init; }
        public double buyMax { get; init; }
        public double buyMin { get; init; }
        public double sellVolume { get; init; }
        public double sellAvg { get; init; }
        public double sellweightedAvg { get; init; }
        public double sellMax { get; init; }
        public double sellMin { get; init; }
        public long RegionOrSystem { get; init; }
        public string UpdateDate { get; init; } = string.Empty;
        public int PRICE_SOURCE { get; init; }
    }
}
