using server.Models;

namespace server.Services.Market;

public interface IMarketService
{
    Task<MarketPricesResponse> GetPricesAsync(MarketPricesRequest request, CancellationToken ct = default);
}
