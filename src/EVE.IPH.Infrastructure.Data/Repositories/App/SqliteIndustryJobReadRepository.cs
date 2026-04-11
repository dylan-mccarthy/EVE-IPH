using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

public sealed class SqliteIndustryJobReadRepository : IIndustryJobReadRepository
{
    private const string UnknownInstaller = "Unknown Installer";
    private const string UnknownActivity = "Unknown Activity";
    private const string UnknownBlueprint = "Unknown Blueprint";
    private const string UnknownOutput = "Unknown Output";
    private const string UnknownOutputType = "Unknown Type";
    private const string UnknownLocation = "Unknown";
    private const string UnknownSystem = "Unknown System";
    private const string UnknownRegion = "Unknown Region";

    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteIndustryJobReadRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public Task<Result<IReadOnlyList<IndustryJobScreenRecord>>> GetViewRecordsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            connection.Open();

            List<IndustryJobRow> jobRows = LoadIndustryJobs(connection);
            if (jobRows.Count == 0)
            {
                return Task.FromResult(Result<IReadOnlyList<IndustryJobScreenRecord>>.Success([]));
            }

            Dictionary<long, string> installerNames = LoadNames(connection, "SELECT CHARACTER_ID, CHARACTER_NAME FROM ESI_CHARACTER_DATA WHERE CHARACTER_ID IN ({0})", jobRows.Select(job => job.InstallerId).Distinct().ToArray());
            Dictionary<int, string> activityNames = LoadActivityNames(connection, jobRows.Select(job => job.ActivityId).Distinct().ToArray());
            List<LocationAssetRow> locationAssets = LoadLocationAssets(connection);
            Dictionary<long, LocationAssetRow> assetsByItemId = locationAssets
                .GroupBy(asset => asset.ItemId)
                .ToDictionary(group => group.Key, group => group.First());

            long[] relevantLocationIds = jobRows
                .SelectMany(job => new[] { job.LocationId, job.BlueprintLocationId, job.OutputLocationId })
                .Concat(locationAssets.Select(asset => asset.LocationId))
                .Where(locationId => locationId != 0)
                .Distinct()
                .ToArray();

            Dictionary<long, StationRow> stations = LoadStations(connection, relevantLocationIds);
            long[] solarSystemIds = relevantLocationIds
                .Concat(stations.Values.Select(station => station.SolarSystemId))
                .Where(locationId => locationId != 0)
                .Distinct()
                .ToArray();
            Dictionary<long, SolarSystemRow> solarSystems = LoadSolarSystems(connection, solarSystemIds);
            Dictionary<long, string> regionNames = LoadNames(connection, "SELECT regionID, regionName FROM REGIONS WHERE regionID IN ({0})", solarSystems.Values.Select(system => system.RegionId).Distinct().ToArray());

            long[] itemTypeIds = jobRows
                .SelectMany(job => new[] { job.BlueprintTypeId, job.ProductTypeId })
                .Concat(locationAssets.Select(asset => asset.TypeId))
                .Where(typeId => typeId != 0)
                .Distinct()
                .ToArray();
            Dictionary<long, ItemLookupRow> itemLookup = LoadItemLookup(connection, itemTypeIds);

            IReadOnlyList<IndustryJobScreenRecord> records = jobRows
                .Select(job => BuildScreenRecord(job, installerNames, activityNames, itemLookup, assetsByItemId, stations, solarSystems, regionNames))
                .ToArray();

            return Task.FromResult(Result<IReadOnlyList<IndustryJobScreenRecord>>.Success(records));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<IReadOnlyList<IndustryJobScreenRecord>>.Failure("DB_ERROR", ex.Message));
        }
    }

    private static List<IndustryJobRow> LoadIndustryJobs(System.Data.IDbConnection connection)
    {
        using System.Data.IDbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT jobID, installerID, locationID, activityID, blueprintTypeID, blueprintLocationID, outputLocationID, runs, licensedRuns, productTypeID, status, startDate, endDate, successfulRuns, JobType FROM INDUSTRY_JOBS ORDER BY endDate DESC, jobID DESC";

        using System.Data.IDataReader reader = command.ExecuteReader();
        List<IndustryJobRow> rows = [];

        while (reader.Read())
        {
            rows.Add(new IndustryJobRow(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.IsDBNull(2) ? 0 : reader.GetInt64(2),
                NormalizeActivityId(reader.GetInt32(3)),
                reader.IsDBNull(4) ? 0 : reader.GetInt64(4),
                reader.IsDBNull(5) ? 0 : reader.GetInt64(5),
                reader.IsDBNull(6) ? 0 : reader.GetInt64(6),
                reader.IsDBNull(7) ? 0 : reader.GetInt64(7),
                reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                reader.IsDBNull(9) ? 0 : reader.GetInt64(9),
                reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                ReadDateTimeOffset(reader, 11),
                ReadDateTimeOffset(reader, 12),
                reader.IsDBNull(13) ? 0 : reader.GetInt32(13),
                reader.IsDBNull(14) || reader.GetInt32(14) == 0 ? IndustryJobScope.Personal : IndustryJobScope.Corporation));
        }

        return rows;
    }

    private static List<LocationAssetRow> LoadLocationAssets(System.Data.IDbConnection connection)
    {
        try
        {
            using System.Data.IDbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT ItemID, LocationID, TypeID, ItemName FROM ASSETS ORDER BY ItemID";

            using System.Data.IDataReader reader = command.ExecuteReader();
            List<LocationAssetRow> assets = [];

            while (reader.Read())
            {
                assets.Add(new LocationAssetRow(
                    reader.GetInt64(0),
                    reader.IsDBNull(1) ? 0 : reader.GetInt64(1),
                    reader.IsDBNull(2) ? 0 : reader.GetInt64(2),
                    reader.IsDBNull(3) ? string.Empty : reader.GetString(3)));
            }

            return assets;
        }
        catch
        {
            return [];
        }
    }

    private static Dictionary<int, string> LoadActivityNames(System.Data.IDbConnection connection, IReadOnlyList<int> activityIds)
    {
        if (activityIds.Count == 0)
        {
            return [];
        }

        using System.Data.IDbCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT activityID, activityName FROM INDUSTRY_ACTIVITIES WHERE activityID IN ({BuildParameters(command, activityIds)})";

        using System.Data.IDataReader reader = command.ExecuteReader();
        Dictionary<int, string> activityNames = [];

        while (reader.Read())
        {
            activityNames[reader.GetInt32(0)] = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        }

        return activityNames;
    }

    private static Dictionary<long, StationRow> LoadStations(System.Data.IDbConnection connection, IReadOnlyList<long> stationIds)
    {
        if (stationIds.Count == 0)
        {
            return [];
        }

        using System.Data.IDbCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT STATION_ID, STATION_NAME, SOLAR_SYSTEM_ID FROM STATIONS WHERE STATION_ID IN ({BuildParameters(command, stationIds)})";

        using System.Data.IDataReader reader = command.ExecuteReader();
        Dictionary<long, StationRow> stations = [];

        while (reader.Read())
        {
            stations[reader.GetInt64(0)] = new StationRow(
                reader.GetInt64(0),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                reader.IsDBNull(2) ? 0 : reader.GetInt64(2));
        }

        return stations;
    }

    private static Dictionary<long, SolarSystemRow> LoadSolarSystems(System.Data.IDbConnection connection, IReadOnlyList<long> solarSystemIds)
    {
        if (solarSystemIds.Count == 0)
        {
            return [];
        }

        using System.Data.IDbCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT solarSystemID, solarSystemName, regionID FROM SOLAR_SYSTEMS WHERE solarSystemID IN ({BuildParameters(command, solarSystemIds)})";

        using System.Data.IDataReader reader = command.ExecuteReader();
        Dictionary<long, SolarSystemRow> solarSystems = [];

        while (reader.Read())
        {
            solarSystems[reader.GetInt64(0)] = new SolarSystemRow(
                reader.GetInt64(0),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                reader.IsDBNull(2) ? 0 : reader.GetInt64(2));
        }

        return solarSystems;
    }

    private static Dictionary<long, ItemLookupRow> LoadItemLookup(System.Data.IDbConnection connection, IReadOnlyList<long> typeIds)
    {
        if (typeIds.Count == 0)
        {
            return [];
        }

        using System.Data.IDbCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT typeID, typeName, groupName FROM ITEM_LOOKUP WHERE typeID IN ({BuildParameters(command, typeIds)})";

        using System.Data.IDataReader reader = command.ExecuteReader();
        Dictionary<long, ItemLookupRow> itemLookup = [];

        while (reader.Read())
        {
            itemLookup[reader.GetInt64(0)] = new ItemLookupRow(
                reader.GetInt64(0),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2));
        }

        return itemLookup;
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

    private static IndustryJobScreenRecord BuildScreenRecord(
        IndustryJobRow job,
        IReadOnlyDictionary<long, string> installerNames,
        IReadOnlyDictionary<int, string> activityNames,
        IReadOnlyDictionary<long, ItemLookupRow> itemLookup,
        IReadOnlyDictionary<long, LocationAssetRow> assetsByItemId,
        IReadOnlyDictionary<long, StationRow> stations,
        IReadOnlyDictionary<long, SolarSystemRow> solarSystems,
        IReadOnlyDictionary<long, string> regionNames)
    {
        ItemLookupRow? blueprint = itemLookup.GetValueOrDefault(job.BlueprintTypeId);
        ItemLookupRow? output = itemLookup.GetValueOrDefault(job.ProductTypeId);
        LocationContext installContext = ResolveInstallContext(job.LocationId, assetsByItemId, stations, solarSystems, regionNames, []);

        return new IndustryJobScreenRecord(
            job.JobId,
            job.InstallerId,
            job.ActivityId,
            job.Status,
            job.StartDate,
            job.EndDate,
            GetValueOrDefault(installerNames, job.InstallerId, UnknownInstaller),
            GetActivityName(job.ActivityId, activityNames),
            GetTypeName(blueprint, UnknownBlueprint),
            output is not null ? GetTypeName(output, UnknownOutput) : GetTypeName(blueprint, UnknownOutput),
            output is not null && !string.IsNullOrWhiteSpace(output.GroupName) ? output.GroupName : UnknownOutputType,
            installContext.SystemName,
            installContext.RegionName,
            job.LicensedRuns,
            job.Runs,
            job.SuccessfulRuns,
            ResolveLocationName(job.BlueprintLocationId, itemLookup, assetsByItemId, stations, solarSystems, []),
            ResolveLocationName(job.OutputLocationId, itemLookup, assetsByItemId, stations, solarSystems, []),
            job.Scope);
    }

    private static string ResolveLocationName(
        long locationId,
        IReadOnlyDictionary<long, ItemLookupRow> itemLookup,
        IReadOnlyDictionary<long, LocationAssetRow> assetsByItemId,
        IReadOnlyDictionary<long, StationRow> stations,
        IReadOnlyDictionary<long, SolarSystemRow> solarSystems,
        HashSet<long> visitedLocationIds)
    {
        if (locationId == 0 || !visitedLocationIds.Add(locationId))
        {
            return UnknownLocation;
        }

        if (stations.TryGetValue(locationId, out StationRow? station) && !string.IsNullOrWhiteSpace(station.Name))
        {
            return station.Name;
        }

        if (solarSystems.TryGetValue(locationId, out SolarSystemRow? solarSystem) && !string.IsNullOrWhiteSpace(solarSystem.Name))
        {
            return solarSystem.Name;
        }

        if (assetsByItemId.TryGetValue(locationId, out LocationAssetRow? asset))
        {
            if (!string.IsNullOrWhiteSpace(asset.ItemName))
            {
                return asset.ItemName;
            }

            if (itemLookup.TryGetValue(asset.TypeId, out ItemLookupRow? assetType) && !string.IsNullOrWhiteSpace(assetType.TypeName))
            {
                return assetType.TypeName;
            }

            return ResolveLocationName(asset.LocationId, itemLookup, assetsByItemId, stations, solarSystems, visitedLocationIds);
        }

        return UnknownLocation;
    }

    private static LocationContext ResolveInstallContext(
        long locationId,
        IReadOnlyDictionary<long, LocationAssetRow> assetsByItemId,
        IReadOnlyDictionary<long, StationRow> stations,
        IReadOnlyDictionary<long, SolarSystemRow> solarSystems,
        IReadOnlyDictionary<long, string> regionNames,
        HashSet<long> visitedLocationIds)
    {
        if (locationId == 0 || !visitedLocationIds.Add(locationId))
        {
            return new LocationContext(UnknownSystem, UnknownRegion);
        }

        if (stations.TryGetValue(locationId, out StationRow? station))
        {
            return CreateLocationContext(station.SolarSystemId, solarSystems, regionNames);
        }

        if (solarSystems.TryGetValue(locationId, out SolarSystemRow? solarSystem))
        {
            return CreateLocationContext(solarSystem.Id, solarSystems, regionNames);
        }

        if (assetsByItemId.TryGetValue(locationId, out LocationAssetRow? asset))
        {
            return ResolveInstallContext(asset.LocationId, assetsByItemId, stations, solarSystems, regionNames, visitedLocationIds);
        }

        return new LocationContext(UnknownSystem, UnknownRegion);
    }

    private static LocationContext CreateLocationContext(
        long solarSystemId,
        IReadOnlyDictionary<long, SolarSystemRow> solarSystems,
        IReadOnlyDictionary<long, string> regionNames)
    {
        if (!solarSystems.TryGetValue(solarSystemId, out SolarSystemRow? solarSystem))
        {
            return new LocationContext(UnknownSystem, UnknownRegion);
        }

        string systemName = string.IsNullOrWhiteSpace(solarSystem.Name) ? UnknownSystem : solarSystem.Name;
        string regionName = GetValueOrDefault(regionNames, solarSystem.RegionId, UnknownRegion);
        return new LocationContext(systemName, regionName);
    }

    private static string GetActivityName(int activityId, IReadOnlyDictionary<int, string> activityNames)
    {
        if (activityNames.TryGetValue(activityId, out string? activityName) && !string.IsNullOrWhiteSpace(activityName))
        {
            return activityName;
        }

        return activityId switch
        {
            1 => "Manufacturing",
            4 => "Research",
            11 => "Reaction",
            _ => UnknownActivity,
        };
    }

    private static string GetTypeName(ItemLookupRow? item, string fallback) =>
        item is not null && !string.IsNullOrWhiteSpace(item.TypeName)
            ? item.TypeName
            : fallback;

    private static string GetValueOrDefault(IReadOnlyDictionary<long, string> values, long id, string fallback) =>
        values.TryGetValue(id, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;

    private static DateTimeOffset? ReadDateTimeOffset(System.Data.IDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        object value = reader.GetValue(ordinal);
        if (value is DateTime dateTime)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
        }

        if (value is string text && DateTimeOffset.TryParse(text, out DateTimeOffset parsed))
        {
            return parsed;
        }

        return null;
    }

    private static int NormalizeActivityId(int activityId) => activityId == 9 ? 11 : activityId;

    private sealed record IndustryJobRow(
        long JobId,
        long InstallerId,
        long LocationId,
        int ActivityId,
        long BlueprintTypeId,
        long BlueprintLocationId,
        long OutputLocationId,
        long Runs,
        int LicensedRuns,
        long ProductTypeId,
        string Status,
        DateTimeOffset? StartDate,
        DateTimeOffset? EndDate,
        int SuccessfulRuns,
        IndustryJobScope Scope);

    private sealed record LocationAssetRow(
        long ItemId,
        long LocationId,
        long TypeId,
        string ItemName);

    private sealed record StationRow(
        long Id,
        string Name,
        long SolarSystemId);

    private sealed record SolarSystemRow(
        long Id,
        string Name,
        long RegionId);

    private sealed record ItemLookupRow(
        long TypeId,
        string TypeName,
        string GroupName);

    private sealed record LocationContext(
        string SystemName,
        string RegionName);
}