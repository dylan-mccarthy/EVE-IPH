using System.IO.Compression;
using System.Text.Json;
using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;
using EVE.IPH.Infrastructure.Settings.Models;

namespace EVE.IPH.UI.Avalonia.Startup;

public sealed class StaticDataBootstrapper
{
    private static readonly (int ActivityId, string ActivityName)[] IndustryActivities =
    [
        (1, "Manufacturing"),
        (3, "Researching Time Efficiency"),
        (4, "Researching Material Efficiency"),
        (5, "Copying"),
        (7, "Reverse Engineering"),
        (8, "Invention"),
        (9, "Reaction"),
        (11, "Reaction"),
    ];

    private readonly ISettingsStore _settingsStore;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly HttpClient _httpClient;

    public StaticDataBootstrapper(
        ISettingsStore settingsStore,
        IDbConnectionFactory connectionFactory,
        HttpClient httpClient)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<StaticDataSettingsModel> EnsureStaticDataAsync(Action<string>? reportProgress = null, CancellationToken cancellationToken = default)
    {
        StaticDataSettingsModel settings = await LoadSettingsAsync(cancellationToken).ConfigureAwait(false);
        bool hasRequiredStaticData = await HasRequiredStaticDataAsync(cancellationToken).ConfigureAwait(false);

        if (settings.ImportedBuildNumber == settings.SupportedBuildNumber && hasRequiredStaticData)
        {
            reportProgress?.Invoke($"Static data already available for build {settings.SupportedBuildNumber}.");
            return settings;
        }

        if (settings.ImportedBuildNumber is null && hasRequiredStaticData)
        {
            StaticDataSettingsModel updatedSettings = settings with
            {
                ImportedBuildNumber = settings.SupportedBuildNumber,
                ImportedAtUtc = DateTimeOffset.UtcNow,
            };

            Result<bool> writeResult = await _settingsStore.WriteAsync(updatedSettings, cancellationToken).ConfigureAwait(false);
            if (writeResult.IsFailure)
            {
                throw new InvalidOperationException(writeResult.Error.Message);
            }

            reportProgress?.Invoke($"Static data already existed locally; recorded supported build {settings.SupportedBuildNumber} in settings.");
            return updatedSettings;
        }

        string archivePath = Path.Combine(Path.GetTempPath(), $"eve-iph-sde-{Guid.NewGuid():N}.zip");

        try
        {
            reportProgress?.Invoke($"Downloading SDE archive for build {settings.SupportedBuildNumber}...");
            await DownloadArchiveAsync(settings.SourceArchiveUrl, archivePath, cancellationToken).ConfigureAwait(false);
            reportProgress?.Invoke("Download complete. Importing static data into SQLite...");

            using FileStream archiveStream = File.OpenRead(archivePath);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.Read, leaveOpen: false);

            long archiveBuildNumber = ReadBuildNumber(archive);
            if (archiveBuildNumber != settings.SupportedBuildNumber)
            {
                throw new InvalidOperationException(
                    $"Configured supported SDE build {settings.SupportedBuildNumber} does not match downloaded archive build {archiveBuildNumber}.");
            }

            await ImportCoreStaticDataAsync(archive, cancellationToken).ConfigureAwait(false);
            reportProgress?.Invoke("Static data import completed. Persisting imported build metadata...");

            StaticDataSettingsModel updatedSettings = settings with
            {
                ImportedBuildNumber = archiveBuildNumber,
                ImportedAtUtc = DateTimeOffset.UtcNow,
            };

            Result<bool> writeResult = await _settingsStore.WriteAsync(updatedSettings, cancellationToken).ConfigureAwait(false);
            if (writeResult.IsFailure)
            {
                throw new InvalidOperationException(writeResult.Error.Message);
            }

            reportProgress?.Invoke($"Static data ready for build {archiveBuildNumber}.");
            return updatedSettings;
        }
        finally
        {
            try
            {
                if (File.Exists(archivePath))
                {
                    File.Delete(archivePath);
                }
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }
    }

    private async Task<StaticDataSettingsModel> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        Maybe<StaticDataSettingsModel> existingSettings = await _settingsStore.ReadAsync<StaticDataSettingsModel>(cancellationToken).ConfigureAwait(false);
        if (existingSettings.HasValue)
        {
            StaticDataSettingsModel value = existingSettings.Value;
            bool shouldRewrite = string.IsNullOrWhiteSpace(value.SourceArchiveUrl) || value.SupportedBuildNumber <= 0;
            if (!shouldRewrite)
            {
                return value;
            }

            StaticDataSettingsModel rewritten = value with
            {
                SourceArchiveUrl = string.IsNullOrWhiteSpace(value.SourceArchiveUrl)
                    ? StaticDataSettingsModel.DefaultSourceArchiveUrl
                    : value.SourceArchiveUrl,
                SupportedBuildNumber = value.SupportedBuildNumber <= 0
                    ? StaticDataSettingsModel.DefaultSupportedBuildNumber
                    : value.SupportedBuildNumber,
            };

            Result<bool> writeResult = await _settingsStore.WriteAsync(rewritten, cancellationToken).ConfigureAwait(false);
            if (writeResult.IsFailure)
            {
                throw new InvalidOperationException(writeResult.Error.Message);
            }

            return rewritten;
        }

        StaticDataSettingsModel defaults = new();
        Result<bool> defaultWriteResult = await _settingsStore.WriteAsync(defaults, cancellationToken).ConfigureAwait(false);
        if (defaultWriteResult.IsFailure)
        {
            throw new InvalidOperationException(defaultWriteResult.Error.Message);
        }

        return defaults;
    }

    private async Task<bool> HasRequiredStaticDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            connection.Open();

            foreach (string tableName in new[] { "INVENTORY_TYPES", "ITEM_LOOKUP", "REGIONS", "SOLAR_SYSTEMS", "STATIONS", "INDUSTRY_ACTIVITIES", "ALL_BLUEPRINTS_FACT", "ALL_BLUEPRINT_MATERIALS_FACT", "INDUSTRY_ACTIVITY_SKILLS" })
            {
                cancellationToken.ThrowIfCancellationRequested();

                using System.Data.IDbCommand command = connection.CreateCommand();
                command.CommandText = $"SELECT COUNT(1) FROM {tableName}";
                object? result = command.ExecuteScalar();
                if (result is null || Convert.ToInt64(result) == 0)
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task DownloadArchiveAsync(string sourceArchiveUrl, string archivePath, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient
            .GetAsync(sourceArchiveUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using FileStream output = new(archivePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
    }

    private async Task ImportCoreStaticDataAsync(ZipArchive archive, CancellationToken cancellationToken)
    {
        Dictionary<int, CategoryRow> categories = await LoadCategoriesAsync(archive, cancellationToken).ConfigureAwait(false);
        Dictionary<int, GroupRow> groups = await LoadGroupsAsync(archive, categories, cancellationToken).ConfigureAwait(false);
        Dictionary<int, string> regionNames = await LoadRegionNamesAsync(archive, cancellationToken).ConfigureAwait(false);
        Dictionary<int, SolarSystemSeed> solarSystems = await LoadSolarSystemsAsync(archive, cancellationToken).ConfigureAwait(false);

        using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
        connection.Open();

        using System.Data.IDbTransaction transaction = connection.BeginTransaction();
        try
        {
            DeleteExistingStaticData(connection, transaction);
            InsertIndustryActivities(connection, transaction);
            InsertGroups(connection, transaction, groups.Values);

            Dictionary<int, string> typeNames = await InsertTypesAsync(archive, connection, transaction, groups, cancellationToken).ConfigureAwait(false);
            await InsertBlueprintsAsync(archive, connection, transaction, typeNames, cancellationToken).ConfigureAwait(false);
            InsertRegions(connection, transaction, regionNames);
            InsertSolarSystems(connection, transaction, solarSystems.Values);
            await InsertStationsAsync(archive, connection, transaction, solarSystems, typeNames, cancellationToken).ConfigureAwait(false);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void DeleteExistingStaticData(System.Data.IDbConnection connection, System.Data.IDbTransaction transaction)
    {
        foreach (string tableName in new[]
                 {
                     "STATIONS",
                     "SOLAR_SYSTEMS",
                     "REGIONS",
                     "ITEM_LOOKUP",
                     "INVENTORY_TYPES",
                     "INVENTORY_CATEGORIES",
                     "INDUSTRY_ACTIVITY_SKILLS",
                     "ALL_BLUEPRINT_MATERIALS_FACT",
                     "ALL_BLUEPRINTS_FACT",
                     "INDUSTRY_ACTIVITIES",
                 })
        {
            using System.Data.IDbCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"DELETE FROM {tableName}";
            command.ExecuteNonQuery();
        }
    }

    private static void InsertIndustryActivities(System.Data.IDbConnection connection, System.Data.IDbTransaction transaction)
    {
        using System.Data.IDbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO INDUSTRY_ACTIVITIES (activityID, activityName) VALUES (@activityID, @activityName)";

        System.Data.IDbDataParameter activityId = command.CreateParameter();
        activityId.ParameterName = "@activityID";
        command.Parameters.Add(activityId);

        System.Data.IDbDataParameter activityName = command.CreateParameter();
        activityName.ParameterName = "@activityName";
        command.Parameters.Add(activityName);

        foreach ((int activityIdValue, string activityNameValue) in IndustryActivities)
        {
            activityId.Value = activityIdValue;
            activityName.Value = activityNameValue;
            command.ExecuteNonQuery();
        }
    }

    private static void InsertGroups(System.Data.IDbConnection connection, System.Data.IDbTransaction transaction, IEnumerable<GroupRow> groups)
    {
        using System.Data.IDbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO INVENTORY_CATEGORIES (groupID, groupName, categoryID) VALUES (@groupID, @groupName, @categoryID)";

        System.Data.IDbDataParameter groupId = command.CreateParameter();
        groupId.ParameterName = "@groupID";
        command.Parameters.Add(groupId);

        System.Data.IDbDataParameter groupName = command.CreateParameter();
        groupName.ParameterName = "@groupName";
        command.Parameters.Add(groupName);

        System.Data.IDbDataParameter categoryId = command.CreateParameter();
        categoryId.ParameterName = "@categoryID";
        command.Parameters.Add(categoryId);

        foreach (GroupRow group in groups.OrderBy(group => group.GroupId))
        {
            groupId.Value = group.GroupId;
            groupName.Value = group.GroupName;
            categoryId.Value = group.CategoryId;
            command.ExecuteNonQuery();
        }
    }

    private static async Task<Dictionary<int, string>> InsertTypesAsync(
        ZipArchive archive,
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        IReadOnlyDictionary<int, GroupRow> groups,
        CancellationToken cancellationToken)
    {
        using StreamReader reader = OpenEntryReader(archive, "types.jsonl");
        using System.Data.IDbCommand inventoryTypeCommand = connection.CreateCommand();
        inventoryTypeCommand.Transaction = transaction;
        inventoryTypeCommand.CommandText = "INSERT INTO INVENTORY_TYPES (typeID, typeName, groupID, volume, portionSize) VALUES (@typeID, @typeName, @groupID, @volume, @portionSize)";

        System.Data.IDbDataParameter typeId = AddParameter(inventoryTypeCommand, "@typeID");
        System.Data.IDbDataParameter typeName = AddParameter(inventoryTypeCommand, "@typeName");
        System.Data.IDbDataParameter groupId = AddParameter(inventoryTypeCommand, "@groupID");
        System.Data.IDbDataParameter volume = AddParameter(inventoryTypeCommand, "@volume");
        System.Data.IDbDataParameter portionSize = AddParameter(inventoryTypeCommand, "@portionSize");

        using System.Data.IDbCommand itemLookupCommand = connection.CreateCommand();
        itemLookupCommand.Transaction = transaction;
        itemLookupCommand.CommandText = "INSERT INTO ITEM_LOOKUP (typeID, typeName, groupName, categoryName) VALUES (@lookupTypeID, @lookupTypeName, @groupName, @categoryName)";

        System.Data.IDbDataParameter lookupTypeId = AddParameter(itemLookupCommand, "@lookupTypeID");
        System.Data.IDbDataParameter lookupTypeName = AddParameter(itemLookupCommand, "@lookupTypeName");
        System.Data.IDbDataParameter lookupGroupName = AddParameter(itemLookupCommand, "@groupName");
        System.Data.IDbDataParameter lookupCategoryName = AddParameter(itemLookupCommand, "@categoryName");

        Dictionary<int, string> typeNames = [];

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement root = document.RootElement;

            int currentTypeId = root.GetProperty("_key").GetInt32();
            int currentGroupId = root.GetProperty("groupID").GetInt32();
            string currentTypeName = ReadEnglishName(root.GetProperty("name"), $"Type {currentTypeId}");
            double currentVolume = TryGetDouble(root, "volume");
            int currentPortionSize = TryGetInt32(root, "portionSize", 1);

            GroupRow group = groups.GetValueOrDefault(currentGroupId)
                ?? new GroupRow(currentGroupId, "Unknown Group", 0, "Unknown Category");

            typeId.Value = currentTypeId;
            typeName.Value = currentTypeName;
            groupId.Value = currentGroupId;
            volume.Value = currentVolume;
            portionSize.Value = currentPortionSize;
            inventoryTypeCommand.ExecuteNonQuery();

            lookupTypeId.Value = currentTypeId;
            lookupTypeName.Value = currentTypeName;
            lookupGroupName.Value = group.GroupName;
            lookupCategoryName.Value = group.CategoryName;
            itemLookupCommand.ExecuteNonQuery();

            typeNames[currentTypeId] = currentTypeName;
        }

        return typeNames;
    }

    private static void InsertRegions(System.Data.IDbConnection connection, System.Data.IDbTransaction transaction, IReadOnlyDictionary<int, string> regionNames)
    {
        using System.Data.IDbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO REGIONS (regionID, regionName) VALUES (@regionID, @regionName)";

        System.Data.IDbDataParameter regionId = AddParameter(command, "@regionID");
        System.Data.IDbDataParameter regionName = AddParameter(command, "@regionName");

        foreach ((int currentRegionId, string currentRegionName) in regionNames.OrderBy(entry => entry.Key))
        {
            regionId.Value = currentRegionId;
            regionName.Value = currentRegionName;
            command.ExecuteNonQuery();
        }
    }

    private static void InsertSolarSystems(System.Data.IDbConnection connection, System.Data.IDbTransaction transaction, IEnumerable<SolarSystemSeed> solarSystems)
    {
        using System.Data.IDbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO SOLAR_SYSTEMS (solarSystemID, solarSystemName, regionID, SECURITY) VALUES (@solarSystemID, @solarSystemName, @regionID, @security)";

        System.Data.IDbDataParameter solarSystemId = AddParameter(command, "@solarSystemID");
        System.Data.IDbDataParameter solarSystemName = AddParameter(command, "@solarSystemName");
        System.Data.IDbDataParameter regionId = AddParameter(command, "@regionID");
        System.Data.IDbDataParameter security = AddParameter(command, "@security");

        foreach (SolarSystemSeed solarSystem in solarSystems.OrderBy(system => system.SolarSystemId))
        {
            solarSystemId.Value = solarSystem.SolarSystemId;
            solarSystemName.Value = solarSystem.Name;
            regionId.Value = solarSystem.RegionId;
            security.Value = solarSystem.Security;
            command.ExecuteNonQuery();
        }
    }

    private static async Task InsertStationsAsync(
        ZipArchive archive,
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        IReadOnlyDictionary<int, SolarSystemSeed> solarSystems,
        IReadOnlyDictionary<int, string> typeNames,
        CancellationToken cancellationToken)
    {
        using StreamReader reader = OpenEntryReader(archive, "npcStations.jsonl");
        using System.Data.IDbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO STATIONS (STATION_ID, STATION_NAME, SOLAR_SYSTEM_ID, regionID) VALUES (@stationID, @stationName, @solarSystemID, @regionID)";

        System.Data.IDbDataParameter stationId = AddParameter(command, "@stationID");
        System.Data.IDbDataParameter stationName = AddParameter(command, "@stationName");
        System.Data.IDbDataParameter solarSystemId = AddParameter(command, "@solarSystemID");
        System.Data.IDbDataParameter regionId = AddParameter(command, "@regionID");

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement root = document.RootElement;

            int currentStationId = root.GetProperty("_key").GetInt32();
            int currentSolarSystemId = root.GetProperty("solarSystemID").GetInt32();
            int currentTypeId = root.GetProperty("typeID").GetInt32();

            SolarSystemSeed solarSystem = solarSystems.GetValueOrDefault(currentSolarSystemId)
                ?? new SolarSystemSeed(currentSolarSystemId, $"System {currentSolarSystemId}", 0, 0);
            string currentStationName = typeNames.GetValueOrDefault(currentTypeId)
                ?? $"Station {currentStationId}";

            stationId.Value = currentStationId;
            stationName.Value = currentStationName;
            solarSystemId.Value = currentSolarSystemId;
            regionId.Value = solarSystem.RegionId;
            command.ExecuteNonQuery();
        }
    }

    private static async Task InsertBlueprintsAsync(
        ZipArchive archive,
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        IReadOnlyDictionary<int, string> typeNames,
        CancellationToken cancellationToken)
    {
        using StreamReader reader = OpenEntryReader(archive, "blueprints.jsonl");

        using System.Data.IDbCommand blueprintCommand = connection.CreateCommand();
        blueprintCommand.Transaction = transaction;
        blueprintCommand.CommandText = "INSERT INTO ALL_BLUEPRINTS_FACT (BLUEPRINT_ID, ITEM_ID, TECH_LEVEL, MAX_PRODUCTION_LIMIT, BASE_PRODUCTION_TIME) VALUES (@blueprintID, @itemID, @techLevel, @maxProductionLimit, @baseProductionTime)";

        System.Data.IDbDataParameter blueprintId = AddParameter(blueprintCommand, "@blueprintID");
        System.Data.IDbDataParameter itemId = AddParameter(blueprintCommand, "@itemID");
        System.Data.IDbDataParameter techLevel = AddParameter(blueprintCommand, "@techLevel");
        System.Data.IDbDataParameter maxProductionLimit = AddParameter(blueprintCommand, "@maxProductionLimit");
        System.Data.IDbDataParameter baseProductionTime = AddParameter(blueprintCommand, "@baseProductionTime");

        using System.Data.IDbCommand materialCommand = connection.CreateCommand();
        materialCommand.Transaction = transaction;
        materialCommand.CommandText = "INSERT INTO ALL_BLUEPRINT_MATERIALS_FACT (BLUEPRINT_ID, MATERIAL_ID, QUANTITY, ACTIVITY) VALUES (@materialBlueprintID, @materialID, @quantity, @activity)";

        System.Data.IDbDataParameter materialBlueprintId = AddParameter(materialCommand, "@materialBlueprintID");
        System.Data.IDbDataParameter materialId = AddParameter(materialCommand, "@materialID");
        System.Data.IDbDataParameter quantity = AddParameter(materialCommand, "@quantity");
        System.Data.IDbDataParameter activity = AddParameter(materialCommand, "@activity");

        using System.Data.IDbCommand skillCommand = connection.CreateCommand();
        skillCommand.Transaction = transaction;
        skillCommand.CommandText = "INSERT INTO INDUSTRY_ACTIVITY_SKILLS (blueprintTypeID, activityID, typeID, level) VALUES (@skillBlueprintID, @skillActivityID, @skillTypeID, @skillLevel)";

        System.Data.IDbDataParameter skillBlueprintId = AddParameter(skillCommand, "@skillBlueprintID");
        System.Data.IDbDataParameter skillActivityId = AddParameter(skillCommand, "@skillActivityID");
        System.Data.IDbDataParameter skillTypeId = AddParameter(skillCommand, "@skillTypeID");
        System.Data.IDbDataParameter skillLevel = AddParameter(skillCommand, "@skillLevel");

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement root = document.RootElement;

            int currentBlueprintId = root.TryGetProperty("blueprintTypeID", out JsonElement blueprintTypeIdElement)
                ? blueprintTypeIdElement.GetInt32()
                : root.GetProperty("_key").GetInt32();
            int currentMaxProductionLimit = TryGetInt32(root, "maxProductionLimit", 0);
            JsonElement activities = root.GetProperty("activities");

            BlueprintFactRow? blueprintFact = ReadBlueprintFact(currentBlueprintId, activities);
            if (blueprintFact is null)
            {
                continue;
            }

            blueprintId.Value = currentBlueprintId;
            itemId.Value = blueprintFact.ProductTypeId;
            techLevel.Value = InferTechLevel(typeNames.GetValueOrDefault(blueprintFact.ProductTypeId));
            maxProductionLimit.Value = currentMaxProductionLimit;
            baseProductionTime.Value = blueprintFact.ManufacturingTime;
            blueprintCommand.ExecuteNonQuery();

            foreach (BlueprintMaterialRow materialRow in ReadMaterialRows(currentBlueprintId, activities)
                         .DistinctBy(row => (row.BlueprintId, row.MaterialTypeId, row.ActivityId)))
            {
                materialBlueprintId.Value = materialRow.BlueprintId;
                materialId.Value = materialRow.MaterialTypeId;
                quantity.Value = materialRow.Quantity;
                activity.Value = materialRow.ActivityId;
                materialCommand.ExecuteNonQuery();
            }

            foreach (BlueprintSkillRow skillRow in ReadSkillRows(currentBlueprintId, activities)
                         .DistinctBy(row => (row.BlueprintId, row.ActivityId, row.SkillTypeId)))
            {
                skillBlueprintId.Value = skillRow.BlueprintId;
                skillActivityId.Value = skillRow.ActivityId;
                skillTypeId.Value = skillRow.SkillTypeId;
                skillLevel.Value = skillRow.Level;
                skillCommand.ExecuteNonQuery();
            }
        }
    }

    private static async Task<Dictionary<int, CategoryRow>> LoadCategoriesAsync(ZipArchive archive, CancellationToken cancellationToken)
    {
        using StreamReader reader = OpenEntryReader(archive, "categories.jsonl");
        Dictionary<int, CategoryRow> categories = [];

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement root = document.RootElement;
            int categoryId = root.GetProperty("_key").GetInt32();
            categories[categoryId] = new CategoryRow(categoryId, ReadEnglishName(root.GetProperty("name"), $"Category {categoryId}"));
        }

        return categories;
    }

    private static async Task<Dictionary<int, GroupRow>> LoadGroupsAsync(ZipArchive archive, IReadOnlyDictionary<int, CategoryRow> categories, CancellationToken cancellationToken)
    {
        using StreamReader reader = OpenEntryReader(archive, "groups.jsonl");
        Dictionary<int, GroupRow> groups = [];

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement root = document.RootElement;
            int groupId = root.GetProperty("_key").GetInt32();
            int categoryId = root.GetProperty("categoryID").GetInt32();
            string groupName = ReadEnglishName(root.GetProperty("name"), $"Group {groupId}");
            string categoryName = categories.TryGetValue(categoryId, out CategoryRow? category)
                ? category.Name
                : "Unknown Category";

            groups[groupId] = new GroupRow(groupId, groupName, categoryId, categoryName);
        }

        return groups;
    }

    private static async Task<Dictionary<int, string>> LoadRegionNamesAsync(ZipArchive archive, CancellationToken cancellationToken)
    {
        using StreamReader reader = OpenEntryReader(archive, "mapRegions.jsonl");
        Dictionary<int, string> regions = [];

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement root = document.RootElement;
            int regionId = root.GetProperty("_key").GetInt32();
            regions[regionId] = ReadEnglishName(root.GetProperty("name"), $"Region {regionId}");
        }

        return regions;
    }

    private static async Task<Dictionary<int, SolarSystemSeed>> LoadSolarSystemsAsync(ZipArchive archive, CancellationToken cancellationToken)
    {
        using StreamReader reader = OpenEntryReader(archive, "mapSolarSystems.jsonl");
        Dictionary<int, SolarSystemSeed> solarSystems = [];

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement root = document.RootElement;
            int solarSystemId = root.GetProperty("_key").GetInt32();
            int regionId = root.GetProperty("regionID").GetInt32();
            string name = ReadEnglishName(root.GetProperty("name"), $"System {solarSystemId}");
            double security = TryGetDouble(root, "securityStatus");
            solarSystems[solarSystemId] = new SolarSystemSeed(solarSystemId, name, regionId, security);
        }

        return solarSystems;
    }

    private static long ReadBuildNumber(ZipArchive archive)
    {
        using StreamReader reader = OpenEntryReader(archive, "_sde.jsonl");
        string line = reader.ReadLine() ?? throw new InvalidOperationException("The SDE archive did not contain metadata in _sde.jsonl.");

        using JsonDocument document = JsonDocument.Parse(line);
        return document.RootElement.GetProperty("buildNumber").GetInt64();
    }

    private static StreamReader OpenEntryReader(ZipArchive archive, string entryName)
    {
        ZipArchiveEntry? entry = archive.GetEntry(entryName);
        if (entry is null)
        {
            throw new InvalidOperationException($"The SDE archive did not contain the required entry '{entryName}'.");
        }

        return new StreamReader(entry.Open());
    }

    private static string ReadEnglishName(JsonElement nameElement, string fallback)
    {
        if (nameElement.ValueKind == JsonValueKind.Object
            && nameElement.TryGetProperty("en", out JsonElement english)
            && english.ValueKind == JsonValueKind.String)
        {
            string? value = english.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return fallback;
    }

    private static double TryGetDouble(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetDouble(out double value))
        {
            return value;
        }

        return 0;
    }

    private static int TryGetInt32(JsonElement element, string propertyName, int fallback)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetInt32(out int value))
        {
            return value;
        }

        return fallback;
    }

    private static System.Data.IDbDataParameter AddParameter(System.Data.IDbCommand command, string parameterName)
    {
        System.Data.IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = parameterName;
        command.Parameters.Add(parameter);
        return parameter;
    }

    private static BlueprintFactRow? ReadBlueprintFact(int blueprintId, JsonElement activities)
    {
        if (!TryGetActivityElement(activities, ActivityType.Manufacturing, out JsonElement manufacturingActivity))
        {
            return null;
        }

        if (!manufacturingActivity.TryGetProperty("products", out JsonElement products)
            || products.ValueKind != JsonValueKind.Array
            || products.GetArrayLength() == 0)
        {
            return null;
        }

        JsonElement firstProduct = products[0];
        if (!TryReadInt32(firstProduct, "typeID", out int productTypeId))
        {
            return null;
        }

        return new BlueprintFactRow(
            blueprintId,
            productTypeId,
            TryGetInt32(manufacturingActivity, "time", 0));
    }

    private static IEnumerable<BlueprintMaterialRow> ReadMaterialRows(int blueprintId, JsonElement activities)
    {
        foreach ((ActivityType activityType, JsonElement activityElement) in EnumerateActivities(activities))
        {
            if (!activityElement.TryGetProperty("materials", out JsonElement materials) || materials.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (JsonElement material in materials.EnumerateArray())
            {
                if (!TryReadInt32(material, "typeID", out int materialTypeId)
                    || !TryReadInt32(material, "quantity", out int materialQuantity))
                {
                    continue;
                }

                yield return new BlueprintMaterialRow(
                    blueprintId,
                    materialTypeId,
                    materialQuantity,
                    (int)activityType);
            }
        }
    }

    private static IEnumerable<BlueprintSkillRow> ReadSkillRows(int blueprintId, JsonElement activities)
    {
        foreach ((ActivityType activityType, JsonElement activityElement) in EnumerateActivities(activities))
        {
            if (!activityElement.TryGetProperty("skills", out JsonElement skills) || skills.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (JsonElement skill in skills.EnumerateArray())
            {
                if (!TryReadInt32(skill, "typeID", out int skillTypeId)
                    || !TryReadInt32(skill, "level", out int skillLevel))
                {
                    continue;
                }

                yield return new BlueprintSkillRow(
                    blueprintId,
                    (int)activityType,
                    skillTypeId,
                    skillLevel);
            }
        }
    }

    private static bool TryReadInt32(JsonElement element, string propertyName, out int value)
    {
        value = default;
        return element.TryGetProperty(propertyName, out JsonElement property)
               && property.ValueKind == JsonValueKind.Number
               && property.TryGetInt32(out value);
    }

    private static IEnumerable<(ActivityType ActivityType, JsonElement ActivityElement)> EnumerateActivities(JsonElement activities)
    {
        foreach ((ActivityType activityType, string propertyName) in new[]
                 {
                     (ActivityType.Manufacturing, "manufacturing"),
                     (ActivityType.ResearchingTimeEfficiency, "research_time"),
                     (ActivityType.ResearchingMaterialEfficiency, "research_material"),
                     (ActivityType.Copying, "copying"),
                     (ActivityType.Invention, "invention"),
                     (ActivityType.Reactions, "reaction"),
                 })
        {
            if (activities.TryGetProperty(propertyName, out JsonElement activityElement))
            {
                yield return (activityType, activityElement);
            }
        }
    }

    private static bool TryGetActivityElement(JsonElement activities, ActivityType activityType, out JsonElement activityElement)
    {
        foreach ((ActivityType currentType, JsonElement currentElement) in EnumerateActivities(activities))
        {
            if (currentType == activityType)
            {
                activityElement = currentElement;
                return true;
            }
        }

        activityElement = default;
        return false;
    }

    private static int InferTechLevel(string? productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return 1;
        }

        if (productName.Contains(" III", StringComparison.Ordinal))
        {
            return 3;
        }

        if (productName.Contains(" II", StringComparison.Ordinal))
        {
            return 2;
        }

        return 1;
    }

    private sealed record CategoryRow(int CategoryId, string Name);

    private sealed record GroupRow(int GroupId, string GroupName, int CategoryId, string CategoryName);

    private sealed record SolarSystemSeed(int SolarSystemId, string Name, int RegionId, double Security);

    private sealed record BlueprintFactRow(int BlueprintId, int ProductTypeId, int ManufacturingTime);

    private sealed record BlueprintMaterialRow(int BlueprintId, int MaterialTypeId, int Quantity, int ActivityId);

    private sealed record BlueprintSkillRow(int BlueprintId, int ActivityId, int SkillTypeId, int Level);
}