using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

public sealed class SqliteAssetReadRepository : IAssetReadRepository
{
    private const string UnknownItem = "Unknown Item";
    private const string UnknownGroup = "Unknown Group";
    private const string UnknownCategory = "Unknown Category";
    private const string UnknownLocation = "Unknown Location";

    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteAssetReadRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public Task<Result<IReadOnlyList<AssetScreenRecord>>> GetHydratedAssetsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            connection.Open();

            List<AssetRow> assets = LoadAssets(connection);
            if (assets.Count == 0)
            {
                return Task.FromResult(Result<IReadOnlyList<AssetScreenRecord>>.Success([]));
            }

            Dictionary<long, AssetTypeRow> typeMetadata = LoadTypeMetadata(connection, assets.Select(asset => asset.TypeId).Distinct().ToArray());
            Dictionary<int, FlagRow> flagMetadata = LoadFlagMetadata(connection, assets.Select(asset => Math.Abs(asset.FlagId)).Distinct().ToArray());
            Dictionary<long, string> stationNames = LoadNames(connection, "SELECT STATION_ID, STATION_NAME FROM STATIONS WHERE STATION_ID IN ({0})", assets.Select(asset => asset.LocationId).Distinct().ToArray());
            Dictionary<long, string> solarSystemNames = LoadNames(connection, "SELECT solarSystemID, solarSystemName FROM SOLAR_SYSTEMS WHERE solarSystemID IN ({0})", assets.Select(asset => asset.LocationId).Distinct().ToArray());
            Dictionary<long, AssetRow> assetsByItemId = assets
                .GroupBy(asset => asset.ItemId)
                .ToDictionary(group => group.Key, group => group.First());

            IReadOnlyList<AssetScreenRecord> records = assets
                .Select(asset => HydrateAsset(asset, typeMetadata, flagMetadata, stationNames, solarSystemNames, assetsByItemId))
                .ToArray();

            return Task.FromResult(Result<IReadOnlyList<AssetScreenRecord>>.Success(records));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<IReadOnlyList<AssetScreenRecord>>.Failure("DB_ERROR", ex.Message));
        }
    }

    private static List<AssetRow> LoadAssets(System.Data.IDbConnection connection)
    {
        using System.Data.IDbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT ID, ItemID, LocationID, TypeID, Quantity, Flag, IsSingleton, IsBPCopy, ItemName FROM ASSETS ORDER BY ID, ItemID";

        using System.Data.IDataReader reader = command.ExecuteReader();
        List<AssetRow> assets = [];

        while (reader.Read())
        {
            assets.Add(new AssetRow(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetInt64(2),
                reader.GetInt64(3),
                reader.GetInt64(4),
                reader.GetInt32(5),
                reader.GetInt32(6) != 0,
                reader.GetInt32(7) == -1,
                reader.IsDBNull(8) ? string.Empty : reader.GetString(8)));
        }

        return assets;
    }

    private static Dictionary<long, AssetTypeRow> LoadTypeMetadata(System.Data.IDbConnection connection, IReadOnlyList<long> typeIds)
    {
        if (typeIds.Count == 0)
        {
            return [];
        }

        using System.Data.IDbCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT typeID, typeName, groupName, categoryName FROM ITEM_LOOKUP WHERE typeID IN ({BuildParameters(command, typeIds)})";

        using System.Data.IDataReader reader = command.ExecuteReader();
        Dictionary<long, AssetTypeRow> metadata = [];

        while (reader.Read())
        {
            metadata[reader.GetInt64(0)] = new AssetTypeRow(
                reader.GetInt64(0),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                reader.IsDBNull(3) ? string.Empty : reader.GetString(3));
        }

        return metadata;
    }

    private static Dictionary<int, FlagRow> LoadFlagMetadata(System.Data.IDbConnection connection, IReadOnlyList<int> flagIds)
    {
        if (flagIds.Count == 0)
        {
            return [];
        }

        try
        {
            using System.Data.IDbCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT FlagID, FlagText, container, sort_order FROM INVENTORY_FLAGS WHERE FlagID IN ({BuildParameters(command, flagIds)})";

            using System.Data.IDataReader reader = command.ExecuteReader();
            Dictionary<int, FlagRow> metadata = [];

            while (reader.Read())
            {
                metadata[reader.GetInt32(0)] = new FlagRow(
                    reader.GetInt32(0),
                    reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    !reader.IsDBNull(2) && Convert.ToBoolean(reader.GetValue(2)),
                    reader.IsDBNull(3) ? -1 : Convert.ToInt32(reader.GetValue(3)));
            }

            return metadata;
        }
        catch
        {
            return [];
        }
    }

    private static Dictionary<long, string> LoadNames(System.Data.IDbConnection connection, string sqlTemplate, IReadOnlyList<long> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        using System.Data.IDbCommand command = connection.CreateCommand();
        command.CommandText = string.Format(sqlTemplate, BuildParameters(command, ids));

        using System.Data.IDataReader reader = command.ExecuteReader();
        Dictionary<long, string> names = [];

        while (reader.Read())
        {
            names[reader.GetInt64(0)] = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        }

        return names;
    }

    private static string BuildParameters<T>(System.Data.IDbCommand command, IReadOnlyList<T> values)
    {
        List<string> names = new(values.Count);

        for (int index = 0; index < values.Count; index++)
        {
            System.Data.IDbDataParameter parameter = command.CreateParameter();
            parameter.ParameterName = "@p" + index;
            parameter.Value = values[index] ?? throw new InvalidOperationException("Null values are not valid SQL parameters.");
            command.Parameters.Add(parameter);
            names.Add(parameter.ParameterName);
        }

        return string.Join(", ", names);
    }

    private static AssetScreenRecord HydrateAsset(
        AssetRow asset,
        IReadOnlyDictionary<long, AssetTypeRow> typeMetadata,
        IReadOnlyDictionary<int, FlagRow> flagMetadata,
        IReadOnlyDictionary<long, string> stationNames,
        IReadOnlyDictionary<long, string> solarSystemNames,
        IReadOnlyDictionary<long, AssetRow> assetsByItemId)
    {
        AssetTypeRow? metadata = typeMetadata.GetValueOrDefault(asset.TypeId);
        string baseTypeName = string.IsNullOrWhiteSpace(metadata?.TypeName) ? UnknownItem : metadata.TypeName;
        string displayTypeName = string.IsNullOrWhiteSpace(asset.ItemName)
            ? baseTypeName
            : asset.ItemName + " (" + baseTypeName + ")";

        FlagRow? flag = flagMetadata.GetValueOrDefault(Math.Abs(asset.FlagId));
        string locationName = ResolveLocationName(asset.LocationId, stationNames, solarSystemNames, assetsByItemId, []);
        string flagText = string.IsNullOrWhiteSpace(flag?.FlagText) ? "Unknown" : flag.FlagText;
        if (flagText is "Space" or "Ship Offline")
        {
            locationName += " (In Solar System)";
        }

        return new AssetScreenRecord(
            asset.OwnerId,
            asset.ItemId,
            asset.LocationId,
            asset.TypeId,
            asset.Quantity,
            asset.FlagId,
            asset.IsSingleton,
            asset.IsBlueprintCopy,
            asset.ItemName,
            displayTypeName,
            string.IsNullOrWhiteSpace(metadata?.GroupName) ? UnknownGroup : metadata.GroupName,
            string.IsNullOrWhiteSpace(metadata?.CategoryName) ? UnknownCategory : metadata.CategoryName,
            locationName,
            flagText,
            flag?.Container ?? false,
            flag?.SortOrder ?? -1);
    }

    private static string ResolveLocationName(
        long locationId,
        IReadOnlyDictionary<long, string> stationNames,
        IReadOnlyDictionary<long, string> solarSystemNames,
        IReadOnlyDictionary<long, AssetRow> assetsByItemId,
        HashSet<long> visitedLocationIds)
    {
        if (locationId == 0)
        {
            return UnknownLocation;
        }

        if (!visitedLocationIds.Add(locationId))
        {
            return UnknownLocation;
        }

        if (stationNames.TryGetValue(locationId, out string? stationName) && !string.IsNullOrWhiteSpace(stationName))
        {
            return stationName;
        }

        if (solarSystemNames.TryGetValue(locationId, out string? solarSystemName) && !string.IsNullOrWhiteSpace(solarSystemName))
        {
            return solarSystemName;
        }

        if (assetsByItemId.TryGetValue(locationId, out AssetRow? parentAsset))
        {
            return ResolveLocationName(parentAsset.LocationId, stationNames, solarSystemNames, assetsByItemId, visitedLocationIds);
        }

        return UnknownLocation;
    }

    private sealed record AssetRow(
        long OwnerId,
        long ItemId,
        long LocationId,
        long TypeId,
        long Quantity,
        int FlagId,
        bool IsSingleton,
        bool IsBlueprintCopy,
        string ItemName);

    private sealed record AssetTypeRow(
        long TypeId,
        string TypeName,
        string GroupName,
        string CategoryName);

    private sealed record FlagRow(
        int FlagId,
        string FlagText,
        bool Container,
        int SortOrder);
}