namespace EveIph.Server.Models;

public record MarketPrice(
    int TypeId,
    int RegionId,
    decimal BuyPrice,      // Highest buy order price
    decimal SellPrice,     // Lowest sell order price
    long Volume,           // 24h trading volume
    DateTime LastUpdated,
    DateTime ExpiresAt
);

public record MarketPriceRequest(
    int[] TypeIds,
    int RegionId = 10000002  // Default: The Forge (Jita)
);

public record MarketPriceResponse(
    Dictionary<int, MarketPrice> Prices
);

public record RefreshPricesResponse(
    int Updated,
    int Failed
);
