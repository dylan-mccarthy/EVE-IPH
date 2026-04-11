using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Returns market prices for a batch of item types from a single price source
/// (ESI, EVEMarketer, Fuzzworks, etc.). Infrastructure projects provide
/// concrete implementations; domain code depends only on this interface.
/// </summary>
public interface IMarketPriceSource
{
    /// <summary>
    /// Fetches the latest sell and buy prices for the given item types in the specified region.
    /// </summary>
    /// <param name="typeIds">The item type IDs to look up.</param>
    /// <param name="regionId">The market region to query.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>
    /// A dictionary mapping each <see cref="TypeId"/> to its <see cref="MarketPrice"/>,
    /// or a failure result if the source could not be reached.
    /// </returns>
    Task<Result<IReadOnlyDictionary<TypeId, MarketPrice>>> GetPricesAsync(
        IEnumerable<TypeId> typeIds,
        RegionId regionId,
        CancellationToken cancellationToken = default);
}

/// <summary>A single price snapshot for one item type.</summary>
/// <param name="TypeId">The item type these prices apply to.</param>
/// <param name="MinSell">The lowest active sell-order price, or <c>null</c> if no sell orders exist.</param>
/// <param name="MaxBuy">The highest active buy-order price, or <c>null</c> if no buy orders exist.</param>
/// <param name="Average">The volume-weighted average price from recent market history.</param>
public sealed record MarketPrice(
    TypeId TypeId,
    double? MinSell,
    double? MaxBuy,
    double? Average);
