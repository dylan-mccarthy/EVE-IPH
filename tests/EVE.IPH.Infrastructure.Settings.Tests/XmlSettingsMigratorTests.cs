using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Settings;
using EVE.IPH.Infrastructure.Settings.Migration;
using EVE.IPH.Infrastructure.Settings.Models;

namespace EVE.IPH.Infrastructure.Settings.Tests;

public sealed class XmlSettingsMigratorTests : IDisposable
{
    private readonly string _legacyDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Settings");
    private readonly string _jsonDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "JsonSettings");

    public void Dispose()
    {
        string legacyParent = Path.GetDirectoryName(_legacyDir)!;
        string jsonParent = Path.GetDirectoryName(_jsonDir)!;

        if (Directory.Exists(legacyParent))
        {
            Directory.Delete(legacyParent, recursive: true);
        }

        if (Directory.Exists(jsonParent))
        {
            Directory.Delete(jsonParent, recursive: true);
        }
    }

    private static string BuildApplicationSettingsXml(
        string checkForUpdates = "True",
        string salesTax = "4.5",
        string region = "The Forge") =>
        $"""
        <?xml version="1.0" encoding="utf-8"?>
        <ApplicationSettings>
          <setting name="CheckforUpdatesonStart">{checkForUpdates}</setting>
          <setting name="BaseSalesTaxRate">{salesTax}</setting>
          <setting name="SVRAveragePriceRegion">{region}</setting>
          <setting name="LoadAssetsonStartup">True</setting>
          <setting name="LoadbpsonStartup">True</setting>
          <setting name="DataExportFormat">Default</setting>
        </ApplicationSettings>
        """;

    private static string BuildShoppingListSettingsXml(
        string alwaysOnTop = "True",
        string fees = "False") =>
        $"""
        <?xml version="1.0" encoding="utf-8"?>
        <ShoppingListSettings>
          <setting name="AlwaysonTop">{alwaysOnTop}</setting>
          <setting name="Fees">{fees}</setting>
          <setting name="DataExportFormat">Default</setting>
        </ShoppingListSettings>
        """;

    [Fact]
    public async Task MigrateAsync_WhenNoXmlFilesExist_CompletesWithoutError()
    {
        Directory.CreateDirectory(_legacyDir);
        JsonSettingsStore store = new(_jsonDir);
        XmlSettingsMigrator migrator = new(_legacyDir, store);

        Func<Task> act = () => migrator.MigrateAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MigrateAsync_WithValidApplicationSettingsXml_WritesSettingsFileAndRenamesXml()
    {
        Directory.CreateDirectory(_legacyDir);
        string xmlPath = Path.Combine(_legacyDir, "ApplicationSettings.xml");
        await File.WriteAllTextAsync(xmlPath, BuildApplicationSettingsXml());
        JsonSettingsStore store = new(_jsonDir);
        XmlSettingsMigrator migrator = new(_legacyDir, store);

        await migrator.MigrateAsync();

        File.Exists(xmlPath).Should().BeFalse("the original XML should be renamed");
        File.Exists(xmlPath + ".migrated").Should().BeTrue("the XML should be renamed to .migrated");
        Directory.Exists(_jsonDir).Should().BeTrue("the JSON settings directory should be created");
    }

    [Fact]
    public async Task MigrateAsync_PreservesSettingValues_FromLegacyXml()
    {
        Directory.CreateDirectory(_legacyDir);
        string xmlPath = Path.Combine(_legacyDir, "ApplicationSettings.xml");
        await File.WriteAllTextAsync(xmlPath, BuildApplicationSettingsXml(
            checkForUpdates: "False",
            salesTax: "3.75",
            region: "Domain"));
        JsonSettingsStore store = new(_jsonDir);
        XmlSettingsMigrator migrator = new(_legacyDir, store);

        await migrator.MigrateAsync();

        Maybe<ApplicationSettingsModel> result = await store.ReadAsync<ApplicationSettingsModel>();
        result.HasValue.Should().BeTrue();
        result.Value.CheckForUpdatesOnStart.Should().BeFalse();
        result.Value.BaseSalesTaxRate.Should().Be(3.75);
        result.Value.SvrAveragePriceRegion.Should().Be("Domain");
    }

    [Fact]
    public async Task MigrateAsync_WhenAlreadyMigrated_DoesNotRunAgain()
    {
        Directory.CreateDirectory(_legacyDir);
        string xmlPath = Path.Combine(_legacyDir, "ApplicationSettings.xml");
        await File.WriteAllTextAsync(xmlPath, BuildApplicationSettingsXml(salesTax: "9.99"));

        JsonSettingsStore store = new(_jsonDir);
        XmlSettingsMigrator migrator = new(_legacyDir, store);

        // First migration
        await migrator.MigrateAsync();
        Maybe<ApplicationSettingsModel> firstRead = await store.ReadAsync<ApplicationSettingsModel>();
        double firstTaxRate = firstRead.Value.BaseSalesTaxRate;

        // Restore the XML (simulating a re-run attempt with different values) but keep the .migrated marker
        await File.WriteAllTextAsync(xmlPath, BuildApplicationSettingsXml(salesTax: "1.23"));

        // Second migration — should be skipped because .migrated exists
        await migrator.MigrateAsync();
        Maybe<ApplicationSettingsModel> secondRead = await store.ReadAsync<ApplicationSettingsModel>();

        secondRead.Value.BaseSalesTaxRate.Should().Be(firstTaxRate, "second run should not overwrite the first migration");
    }

    [Fact]
    public async Task MigrateAsync_WithShoppingListXml_WritesAndRenamesCorrectly()
    {
        Directory.CreateDirectory(_legacyDir);
        string xmlPath = Path.Combine(_legacyDir, "ShoppingListSettings.xml");
        await File.WriteAllTextAsync(xmlPath, BuildShoppingListSettingsXml(alwaysOnTop: "True", fees: "True"));
        JsonSettingsStore store = new(_jsonDir);
        XmlSettingsMigrator migrator = new(_legacyDir, store);

        await migrator.MigrateAsync();

        Maybe<ShoppingListSettingsModel> result = await store.ReadAsync<ShoppingListSettingsModel>();
        result.HasValue.Should().BeTrue();
        result.Value.AlwaysOnTop.Should().BeTrue();
        result.Value.Fees.Should().BeTrue();
        File.Exists(xmlPath + ".migrated").Should().BeTrue();
    }
}
