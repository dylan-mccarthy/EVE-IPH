namespace server.Services.Market;

public interface IMarketOrdersService
{
    Task<List<MarketOrder>> GetMarketOrdersAsync(long characterId, CancellationToken ct = default);
}

public sealed record MarketOrder(
    long OrderId,
    long TypeId,
    long LocationId,
    long RegionId,
    string VolumeTotal,
    string VolumeRemain,
    string MinVolume,
    decimal Price,
    bool IsBuyOrder,
    string Duration,
    DateTimeOffset Issued,
    string Range
);
