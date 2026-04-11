using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Market.Services;

/// <summary>
/// Coordinates market price retrieval with cache reuse for a selected source.
/// </summary>
public interface IMarketService
{
    Task<Result<IReadOnlyDictionary<TypeId, MarketPrice>>> GetPricesAsync(
        IEnumerable<TypeId> typeIds,
        RegionId regionId,
        TimeSpan cacheDuration,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyDictionary<TypeId, MarketPrice>>> GetPricesAsync(
        IEnumerable<TypeId> typeIds,
        RegionId regionId,
        MarketPriceSourceKind sourceKind,
        TimeSpan cacheDuration,
        CancellationToken cancellationToken = default);
}