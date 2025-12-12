using Microsoft.Data.Sqlite;

namespace server.Services.IndustryCosts;

public interface IIndustryCostService
{
    Task<Dictionary<int, decimal>> GetAdjustedPricesAsync(IEnumerable<int> typeIds, CancellationToken ct = default);
}
