using server.Models;

namespace server.Services.Market;

public sealed class MarketService : IMarketService
{
    public Task<MarketPricesResponse> GetPricesAsync(MarketPricesRequest request, CancellationToken ct = default)
    {
        // TODO: Implement market price retrieval (ESI/3rd-party)
        var empty = new List<MarketPrice>();
        return Task.FromResult(new MarketPricesResponse(empty, request.Region, request.System));
    }
}
