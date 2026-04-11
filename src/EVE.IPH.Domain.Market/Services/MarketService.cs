using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Market.Services;

/// <summary>
/// Uses cached market prices when they are still fresh and refreshes missing or stale items from the active source.
/// </summary>
public sealed class MarketService(
    IMarketCacheRepository marketCacheRepository,
    IMarketPriceSourceResolver marketPriceSourceResolver,
    IMarketPriceSourcePreferenceProvider marketPriceSourcePreferenceProvider,
    TimeProvider timeProvider) : IMarketService
{
    private readonly IMarketCacheRepository _marketCacheRepository = marketCacheRepository ?? throw new ArgumentNullException(nameof(marketCacheRepository));
    private readonly IMarketPriceSourceResolver _marketPriceSourceResolver = marketPriceSourceResolver ?? throw new ArgumentNullException(nameof(marketPriceSourceResolver));
    private readonly IMarketPriceSourcePreferenceProvider _marketPriceSourcePreferenceProvider = marketPriceSourcePreferenceProvider ?? throw new ArgumentNullException(nameof(marketPriceSourcePreferenceProvider));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<Result<IReadOnlyDictionary<TypeId, MarketPrice>>> GetPricesAsync(
        IEnumerable<TypeId> typeIds,
        RegionId regionId,
        TimeSpan cacheDuration,
        CancellationToken cancellationToken = default)
    {
        Result<MarketPriceSourceKind> selectedSource = await _marketPriceSourcePreferenceProvider
            .GetSelectedSourceAsync(cancellationToken)
            .ConfigureAwait(false);

        if (selectedSource.IsFailure)
        {
            return Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Failure(selectedSource.Error);
        }

        return await GetPricesAsync(typeIds, regionId, selectedSource.Value, cacheDuration, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<IReadOnlyDictionary<TypeId, MarketPrice>>> GetPricesAsync(
        IEnumerable<TypeId> typeIds,
        RegionId regionId,
        MarketPriceSourceKind sourceKind,
        TimeSpan cacheDuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(typeIds);

        if (cacheDuration < TimeSpan.Zero)
        {
            return Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Failure("INVALID_CACHE_DURATION", "Cache duration must be zero or positive.");
        }

        List<TypeId> distinctTypeIds = typeIds.Distinct().ToList();
        if (distinctTypeIds.Count == 0)
        {
            return Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Success(new Dictionary<TypeId, MarketPrice>());
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        int priceSource = (int)sourceKind;
        IMarketPriceSource marketPriceSource = _marketPriceSourceResolver.Resolve(sourceKind);
        Dictionary<TypeId, MarketPrice> resolvedPrices = [];
        List<TypeId> missingTypeIds = [];

        foreach (TypeId typeId in distinctTypeIds)
        {
            Maybe<MarketCacheRecord> cached = await _marketCacheRepository
                .GetAsync(typeId, regionId.Value, priceSource, cancellationToken)
                .ConfigureAwait(false);

            if (cached.HasValue && !IsExpired(cached.Value, now, cacheDuration))
            {
                resolvedPrices[typeId] = MapPrice(cached.Value);
                continue;
            }

            missingTypeIds.Add(typeId);
        }

        if (missingTypeIds.Count == 0)
        {
            return Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Success(resolvedPrices);
        }

        Result<IReadOnlyDictionary<TypeId, MarketPrice>> fetchedPrices = await marketPriceSource
            .GetPricesAsync(missingTypeIds, regionId, cancellationToken)
            .ConfigureAwait(false);

        if (fetchedPrices.IsFailure)
        {
            return Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Failure(fetchedPrices.Error);
        }

        foreach ((TypeId typeId, MarketPrice price) in fetchedPrices.Value)
        {
            resolvedPrices[typeId] = price;

            Result<MarketCacheRecord> upsert = await _marketCacheRepository
                .UpsertAsync(MapRecord(price, regionId.Value, priceSource, now.UtcDateTime), cancellationToken)
                .ConfigureAwait(false);

            if (upsert.IsFailure)
            {
                return Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Failure(upsert.Error);
            }
        }

        return Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Success(resolvedPrices);
    }

    private static bool IsExpired(MarketCacheRecord record, DateTimeOffset now, TimeSpan cacheDuration) =>
        now.UtcDateTime - record.UpdateDate > cacheDuration;

    private static MarketPrice MapPrice(MarketCacheRecord record) => new(
        record.TypeId,
        record.SellMin > 0 ? record.SellMin : null,
        record.BuyMax > 0 ? record.BuyMax : null,
        record.SellWeightedAvg > 0 ? record.SellWeightedAvg : null);

    private static MarketCacheRecord MapRecord(MarketPrice price, long regionId, int priceSource, DateTime updateDate) => new(
        price.TypeId,
        BuyVolume: 0,
        BuyAvg: price.Average ?? 0,
        BuyWeightedAvg: price.Average ?? 0,
        BuyMax: price.MaxBuy ?? 0,
        BuyMin: price.MaxBuy ?? 0,
        SellVolume: 0,
        SellAvg: price.Average ?? 0,
        SellWeightedAvg: price.Average ?? 0,
        SellMax: price.MinSell ?? 0,
        SellMin: price.MinSell ?? 0,
        RegionOrSystem: regionId,
        UpdateDate: updateDate,
        PriceSource: priceSource);
}