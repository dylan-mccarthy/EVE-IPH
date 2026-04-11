using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Caches market price data in the application database to reduce ESI calls.
/// </summary>
public interface IMarketCacheRepository
{
    Task<Maybe<MarketCacheRecord>> GetAsync(TypeId typeId, long regionOrSystem, int priceSource, CancellationToken cancellationToken = default);
    Task<Result<MarketCacheRecord>> UpsertAsync(MarketCacheRecord record, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(TypeId typeId, long regionOrSystem, int priceSource, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>A cached set of market statistics for a type in a region or system.</summary>
public sealed record MarketCacheRecord(
    TypeId TypeId,
    double BuyVolume,
    double BuyAvg,
    double BuyWeightedAvg,
    double BuyMax,
    double BuyMin,
    double SellVolume,
    double SellAvg,
    double SellWeightedAvg,
    double SellMax,
    double SellMin,
    long RegionOrSystem,
    DateTime UpdateDate,
    int PriceSource);
