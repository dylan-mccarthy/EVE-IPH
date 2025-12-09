using System.Data;
using Microsoft.Data.Sqlite;
using server.Infrastructure;

namespace server.Services.SDE;

public sealed class SDEService : ISDEService
{
    private readonly ISqliteConnectionFactory _dbFactory;
    private readonly ILogger<SDEService> _logger;

    public SDEService(ISqliteConnectionFactory dbFactory, ILogger<SDEService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<TypeInfo?> GetTypeInfoAsync(long typeId, CancellationToken ct = default)
    {
        try
        {
            await using var conn = _dbFactory.Create();
            await conn.OpenAsync(ct);

            const string sql = @"
                SELECT 
                    IT.typeID, 
                    IT.typeName, 
                    IG.groupName, 
                    IC.categoryName
                FROM INVENTORY_TYPES IT
                INNER JOIN INVENTORY_GROUPS IG ON IT.groupID = IG.groupID
                INNER JOIN INVENTORY_CATEGORIES IC ON IG.categoryID = IC.categoryID
                WHERE IT.typeID = @typeId";

            await using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@typeId", typeId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            
            if (await reader.ReadAsync(ct))
            {
                return new TypeInfo(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3)
                );
            }

            _logger.LogWarning("Type ID {TypeId} not found in INVENTORY_TYPES", typeId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting type info for type ID {TypeId}", typeId);
            return null;
        }
    }

    public async Task<Dictionary<long, TypeInfo>> GetTypeInfoBatchAsync(IEnumerable<long> typeIds, CancellationToken ct = default)
    {
        var result = new Dictionary<long, TypeInfo>();
        var typeIdList = typeIds.ToList();

        if (typeIdList.Count == 0)
            return result;

        try
        {
            await using var conn = _dbFactory.Create();
            await conn.OpenAsync(ct);

            // Build parameterized query for batch lookup
            var parameters = string.Join(",", typeIdList.Select((_, i) => $"@id{i}"));
            var sql = $@"
                SELECT 
                    IT.typeID, 
                    IT.typeName, 
                    IG.groupName, 
                    IC.categoryName
                FROM INVENTORY_TYPES IT
                INNER JOIN INVENTORY_GROUPS IG ON IT.groupID = IG.groupID
                INNER JOIN INVENTORY_CATEGORIES IC ON IG.categoryID = IC.categoryID
                WHERE IT.typeID IN ({parameters})";

            await using var cmd = new SqliteCommand(sql, conn);
            for (int i = 0; i < typeIdList.Count; i++)
            {
                cmd.Parameters.AddWithValue($"@id{i}", typeIdList[i]);
            }

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            
            while (await reader.ReadAsync(ct))
            {
                var typeInfo = new TypeInfo(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3)
                );
                result[typeInfo.TypeId] = typeInfo;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch type info");
            return result;
        }
    }

    public async Task<string> GetLocationNameAsync(long locationId, CancellationToken ct = default)
    {
        try
        {
            await using var conn = _dbFactory.Create();
            await conn.OpenAsync(ct);

            // First try STATIONS table
            const string stationSql = @"
                SELECT S.STATION_NAME, SS.solarSystemName, R.regionName
                FROM STATIONS S
                LEFT JOIN SOLAR_SYSTEMS SS ON S.SOLAR_SYSTEM_ID = SS.solarSystemID
                LEFT JOIN REGIONS R ON SS.regionID = R.regionID
                WHERE S.STATION_ID = @locationId";

            await using var stationCmd = new SqliteCommand(stationSql, conn);
            stationCmd.Parameters.AddWithValue("@locationId", locationId);

            await using var stationReader = await stationCmd.ExecuteReaderAsync(ct);
            if (await stationReader.ReadAsync(ct))
            {
                var stationName = stationReader.GetString(0);
                var systemName = stationReader.IsDBNull(1) ? null : stationReader.GetString(1);
                var regionName = stationReader.IsDBNull(2) ? null : stationReader.GetString(2);
                
                if (systemName != null && regionName != null)
                {
                    return $"{stationName} ({systemName}, {regionName})";
                }
                return stationName;
            }

            // If not found in stations, try INVENTORY_TYPES (for structures/containers)
            const string typeSql = "SELECT typeName FROM INVENTORY_TYPES WHERE typeID = @locationId";
            await using var typeCmd = new SqliteCommand(typeSql, conn);
            typeCmd.Parameters.AddWithValue("@locationId", locationId);

            var typeName = await typeCmd.ExecuteScalarAsync(ct) as string;
            if (typeName != null)
            {
                return typeName;
            }

            return $"Location {locationId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location name for location ID {LocationId}", locationId);
            return $"Location {locationId}";
        }
    }

    public async Task<string> GetActivityNameAsync(int activityId, CancellationToken ct = default)
    {
        try
        {
            await using var conn = _dbFactory.Create();
            await conn.OpenAsync(ct);

            const string sql = "SELECT activityName FROM INDUSTRY_ACTIVITIES WHERE activityID = @activityId";
            await using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@activityId", activityId);

            var activityName = await cmd.ExecuteScalarAsync(ct) as string;
            return activityName ?? $"Activity {activityId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity name for activity ID {ActivityId}", activityId);
            return $"Activity {activityId}";
        }
    }
}
