using Microsoft.Data.Sqlite;
using server.Infrastructure;

namespace server.Services.IndustryCosts;

public sealed class IndustryCostService : IIndustryCostService
{
    private readonly ISqliteConnectionFactory _connections;

    public IndustryCostService(ISqliteConnectionFactory connections)
    {
        _connections = connections;
    }

    public async Task<Dictionary<int, decimal>> GetAdjustedPricesAsync(IEnumerable<int> typeIds, CancellationToken ct = default)
    {
        var ids = typeIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        await using var conn = _connections.Create();
        await conn.OpenAsync(ct);

        // Build a parameterized IN clause: (@id0,@id1,...)
        var parameters = ids.Select((_, i) => $"@id{i}").ToArray();
        var sql = $@"
            SELECT ITEM_ID, ADJUSTED_PRICE
            FROM ITEM_PRICES_FACT
            WHERE ITEM_ID IN ({string.Join(",", parameters)});";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        for (var i = 0; i < ids.Count; i++)
        {
            cmd.Parameters.AddWithValue(parameters[i], ids[i]);
        }

        var result = new Dictionary<int, decimal>(capacity: ids.Count);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var itemId = reader.GetInt32(0);
            var adjustedPrice = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetDouble(1));
            result[itemId] = adjustedPrice;
        }

        return result;
    }
}
