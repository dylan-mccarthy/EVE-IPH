namespace server.Models;

public sealed record MarketPricesRequest(IReadOnlyList<long> TypeIds, string Region = "The Forge", string System = "Jita");
