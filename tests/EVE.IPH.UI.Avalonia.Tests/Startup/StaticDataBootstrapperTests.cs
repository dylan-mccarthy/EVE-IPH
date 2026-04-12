using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;
using EVE.IPH.Infrastructure.Data.Migrations;
using EVE.IPH.Infrastructure.Settings;
using EVE.IPH.Infrastructure.Settings.Models;
using EVE.IPH.UI.Avalonia.Startup;

namespace EVE.IPH.UI.Avalonia.Tests.Startup;

public sealed class StaticDataBootstrapperTests : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly string _settingsDirectory;
    private readonly string _databasePath;

    public StaticDataBootstrapperTests()
    {
        _settingsDirectory = Path.Combine(_rootDirectory, "settings");
        _databasePath = Path.Combine(_rootDirectory, "eveiph.sqlite");
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task EnsureStaticDataAsync_WhenConfigMissing_ImportsCoreTablesAndPersistsVersion()
    {
        IDbConnectionFactory connectionFactory = await CreateMigratedDatabaseAsync();
        JsonSettingsStore settingsStore = new(_settingsDirectory);
        CountingHttpMessageHandler handler = new(CreateArchiveBytes());
        StaticDataBootstrapper bootstrapper = new(settingsStore, connectionFactory, new HttpClient(handler));

        StaticDataSettingsModel result = await bootstrapper.EnsureStaticDataAsync();
        Maybe<StaticDataSettingsModel> savedSettings = await settingsStore.ReadAsync<StaticDataSettingsModel>();

        handler.RequestCount.Should().Be(1);
        result.ImportedBuildNumber.Should().Be(StaticDataSettingsModel.DefaultSupportedBuildNumber);
        savedSettings.HasValue.Should().BeTrue();
        savedSettings.Value.ImportedBuildNumber.Should().Be(StaticDataSettingsModel.DefaultSupportedBuildNumber);
        savedSettings.Value.SourceArchiveUrl.Should().Be(StaticDataSettingsModel.DefaultSourceArchiveUrl);
        (await GetCountAsync(connectionFactory, "INVENTORY_TYPES")).Should().BeGreaterThan(0);
        (await GetCountAsync(connectionFactory, "ITEM_LOOKUP")).Should().BeGreaterThan(0);
        (await GetCountAsync(connectionFactory, "REGIONS")).Should().BeGreaterThan(0);
        (await GetCountAsync(connectionFactory, "SOLAR_SYSTEMS")).Should().BeGreaterThan(0);
        (await GetCountAsync(connectionFactory, "STATIONS")).Should().BeGreaterThan(0);
        (await GetCountAsync(connectionFactory, "INDUSTRY_ACTIVITIES")).Should().BeGreaterThan(0);
        (await GetCountAsync(connectionFactory, "ALL_BLUEPRINTS_FACT")).Should().BeGreaterThan(0);
        (await GetCountAsync(connectionFactory, "ALL_BLUEPRINT_MATERIALS_FACT")).Should().BeGreaterThan(0);
        (await GetCountAsync(connectionFactory, "INDUSTRY_ACTIVITY_SKILLS")).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EnsureStaticDataAsync_WhenVersionAlreadyImported_SkipsDownload()
    {
        IDbConnectionFactory connectionFactory = await CreateMigratedDatabaseAsync();
        JsonSettingsStore settingsStore = new(_settingsDirectory);

        StaticDataBootstrapper initialBootstrapper = new(settingsStore, connectionFactory, new HttpClient(new CountingHttpMessageHandler(CreateArchiveBytes())));
        await initialBootstrapper.EnsureStaticDataAsync();

        CountingHttpMessageHandler secondHandler = new(CreateArchiveBytes());
        StaticDataBootstrapper secondBootstrapper = new(settingsStore, connectionFactory, new HttpClient(secondHandler));

        StaticDataSettingsModel result = await secondBootstrapper.EnsureStaticDataAsync();

        secondHandler.RequestCount.Should().Be(0);
        result.ImportedBuildNumber.Should().Be(StaticDataSettingsModel.DefaultSupportedBuildNumber);
    }

    [Fact]
    public async Task EnsureStaticDataAsync_WhenStaticDataExistsButSettingsAreMissing_StampsVersionWithoutDownload()
    {
        IDbConnectionFactory connectionFactory = await CreateMigratedDatabaseAsync();
        string initialSettingsDirectory = Path.Combine(_rootDirectory, "settings-initial");
        JsonSettingsStore initialSettingsStore = new(initialSettingsDirectory);

        StaticDataBootstrapper initialBootstrapper = new(initialSettingsStore, connectionFactory, new HttpClient(new CountingHttpMessageHandler(CreateArchiveBytes())));
        await initialBootstrapper.EnsureStaticDataAsync();

        string missingSettingsDirectory = Path.Combine(_rootDirectory, "settings-missing");
        JsonSettingsStore missingSettingsStore = new(missingSettingsDirectory);
        CountingHttpMessageHandler handler = new(CreateArchiveBytes());
        StaticDataBootstrapper bootstrapper = new(missingSettingsStore, connectionFactory, new HttpClient(handler));

        StaticDataSettingsModel result = await bootstrapper.EnsureStaticDataAsync();
        Maybe<StaticDataSettingsModel> savedSettings = await missingSettingsStore.ReadAsync<StaticDataSettingsModel>();

        handler.RequestCount.Should().Be(0);
        result.ImportedBuildNumber.Should().Be(StaticDataSettingsModel.DefaultSupportedBuildNumber);
        savedSettings.HasValue.Should().BeTrue();
        savedSettings.Value.ImportedBuildNumber.Should().Be(StaticDataSettingsModel.DefaultSupportedBuildNumber);
    }

    [Fact]
    public async Task EnsureStaticDataAsync_WhenArchiveContainsDuplicateSkillRows_DeduplicatesImport()
    {
        IDbConnectionFactory connectionFactory = await CreateMigratedDatabaseAsync();
        JsonSettingsStore settingsStore = new(_settingsDirectory);
        CountingHttpMessageHandler handler = new(CreateArchiveBytes(includeDuplicateSkill: true));
        StaticDataBootstrapper bootstrapper = new(settingsStore, connectionFactory, new HttpClient(handler));

        StaticDataSettingsModel result = await bootstrapper.EnsureStaticDataAsync();

        handler.RequestCount.Should().Be(1);
        result.ImportedBuildNumber.Should().Be(StaticDataSettingsModel.DefaultSupportedBuildNumber);
        (await GetCountAsync(connectionFactory, "INDUSTRY_ACTIVITY_SKILLS")).Should().Be(2);
    }

    [Fact]
    public async Task EnsureStaticDataAsync_WhenBlueprintManufacturingProductsAreMissing_SkipsMalformedBlueprint()
    {
        IDbConnectionFactory connectionFactory = await CreateMigratedDatabaseAsync();
        JsonSettingsStore settingsStore = new(_settingsDirectory);
        CountingHttpMessageHandler handler = new(CreateArchiveBytes(includeMalformedManufacturingBlueprint: true));
        StaticDataBootstrapper bootstrapper = new(settingsStore, connectionFactory, new HttpClient(handler));

        StaticDataSettingsModel result = await bootstrapper.EnsureStaticDataAsync();

        handler.RequestCount.Should().Be(1);
        result.ImportedBuildNumber.Should().Be(StaticDataSettingsModel.DefaultSupportedBuildNumber);
        (await GetCountAsync(connectionFactory, "ALL_BLUEPRINTS_FACT")).Should().Be(1);
    }

    private async Task<IDbConnectionFactory> CreateMigratedDatabaseAsync()
    {
        Directory.CreateDirectory(_rootDirectory);

        IDbConnectionFactory connectionFactory = new SqliteConnectionFactory($"Data Source={_databasePath};Pooling=False");
        SqliteMigrationRunner migrationRunner = new(connectionFactory);
        await migrationRunner.RunAsync();
        return connectionFactory;
    }

    private static async Task<long> GetCountAsync(IDbConnectionFactory connectionFactory, string tableName)
    {
        using System.Data.IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();

        using System.Data.IDbCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(1) FROM {tableName}";
        object? result = command.ExecuteScalar();
        return Convert.ToInt64(result ?? 0);
    }

    private static byte[] CreateArchiveBytes(bool includeDuplicateSkill = false, bool includeMalformedManufacturingBlueprint = false)
    {
        using MemoryStream stream = new();
        using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "_sde.jsonl", "{\"_key\":\"sde\",\"buildNumber\":3294658,\"releaseDate\":\"2026-04-09T11:29:37Z\"}\n");
            AddEntry(archive, "categories.jsonl", "{\"_key\":6,\"name\":{\"en\":\"Ship\"}}\n{\"_key\":9,\"name\":{\"en\":\"Blueprint\"}}\n");
            AddEntry(archive, "groups.jsonl", "{\"_key\":25,\"categoryID\":6,\"name\":{\"en\":\"Frigate\"}}\n{\"_key\":441,\"categoryID\":9,\"name\":{\"en\":\"Blueprint\"}}\n{\"_key\":15,\"categoryID\":6,\"name\":{\"en\":\"Station\"}}\n");
            AddEntry(archive, "types.jsonl", "{\"_key\":587,\"groupID\":25,\"name\":{\"en\":\"Rifter\"},\"portionSize\":1,\"volume\":28500}\n{\"_key\":681,\"groupID\":441,\"name\":{\"en\":\"Rifter Blueprint\"},\"portionSize\":1}\n{\"_key\":1531,\"groupID\":15,\"name\":{\"en\":\"Caldari Administrative Station\"},\"portionSize\":1}\n");
            AddEntry(archive, "mapRegions.jsonl", "{\"_key\":10000002,\"name\":{\"en\":\"The Forge\"}}\n");
            AddEntry(archive, "mapSolarSystems.jsonl", "{\"_key\":30000142,\"regionID\":10000002,\"securityStatus\":0.946,\"name\":{\"en\":\"Jita\"}}\n");
            AddEntry(archive, "npcStations.jsonl", "{\"_key\":60003760,\"solarSystemID\":30000142,\"typeID\":1531}\n");
            string manufacturingSkills = includeDuplicateSkill
                ? "[{\"level\":1,\"typeID\":3380},{\"level\":1,\"typeID\":3380}]"
                : "[{\"level\":1,\"typeID\":3380}]";
            string blueprintsContent = $"{{\"_key\":681,\"activities\":{{\"manufacturing\":{{\"materials\":[{{\"quantity\":86,\"typeID\":38}}],\"products\":[{{\"quantity\":1,\"typeID\":587}}],\"skills\":{manufacturingSkills},\"time\":600}},\"invention\":{{\"materials\":[{{\"quantity\":2,\"typeID\":20416}}],\"products\":[{{\"quantity\":1,\"typeID\":39581,\"probability\":0.3}}],\"skills\":[{{\"level\":1,\"typeID\":11442}}],\"time\":63900}},\"research_material\":{{\"time\":210}},\"research_time\":{{\"time\":210}}}},\"blueprintTypeID\":681,\"maxProductionLimit\":300}}\n";
            if (includeMalformedManufacturingBlueprint)
            {
                blueprintsContent += "{\"_key\":682,\"activities\":{\"manufacturing\":{\"materials\":[{\"quantity\":1,\"typeID\":38}],\"skills\":[{\"level\":1,\"typeID\":3380}],\"time\":300}},\"blueprintTypeID\":682,\"maxProductionLimit\":1}\n";
            }

            AddEntry(archive, "blueprints.jsonl", blueprintsContent);
        }

        return stream.ToArray();
    }

    private static void AddEntry(ZipArchive archive, string name, string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name);
        using StreamWriter writer = new(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private sealed class CountingHttpMessageHandler : HttpMessageHandler
    {
        private readonly byte[] _archiveBytes;

        public CountingHttpMessageHandler(byte[] archiveBytes)
        {
            _archiveBytes = archiveBytes;
        }

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_archiveBytes),
            };

            return Task.FromResult(response);
        }
    }
}