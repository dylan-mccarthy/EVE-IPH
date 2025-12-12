using EveIph.Server.Models;

namespace EveIph.Server.Services.Market;

public interface IMarketPriceService
{
    /// <summary>
    /// Get cached price for a single type without refreshing from ESI.
    /// Returns null if missing or expired.
    /// </summary>
    Task<MarketPrice?> GetCachedPriceAsync(int typeId, int regionId = 10000002);

    /// <summary>
    /// Get cached prices for multiple types without refreshing from ESI.
    /// Missing or expired entries are omitted.
    /// </summary>
    Task<Dictionary<int, MarketPrice>> GetCachedPricesAsync(IEnumerable<int> typeIds, int regionId = 10000002);

    /// <summary>
    /// Get cached price for a single type, fetching from ESI if expired
    /// </summary>
    Task<MarketPrice?> GetPriceAsync(int typeId, int regionId = 10000002);
    
    /// <summary>
    /// Get cached prices for multiple types, fetching from ESI if expired
    /// </summary>
    Task<Dictionary<int, MarketPrice>> GetPricesAsync(IEnumerable<int> typeIds, int regionId = 10000002);
    
    /// <summary>
    /// Force refresh prices from ESI for given types
    /// </summary>
    Task<(int updated, int failed)> RefreshPricesAsync(IEnumerable<int> typeIds, int regionId = 10000002);
}
