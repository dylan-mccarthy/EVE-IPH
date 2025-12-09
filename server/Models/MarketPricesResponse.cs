namespace server.Models;

public sealed record MarketPricesResponse(IReadOnlyList<MarketPrice> Prices, string Region, string System);

public sealed record MarketPrice(long TypeId, decimal MinSell, decimal MaxBuy, DateTimeOffset AsOfUtc);
