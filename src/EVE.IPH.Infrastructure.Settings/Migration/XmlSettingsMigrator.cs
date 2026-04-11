using System.Globalization;
using System.Xml;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Infrastructure.Settings.Models;

namespace EVE.IPH.Infrastructure.Settings.Migration;

/// <summary>
/// One-shot migrator that reads legacy XML settings files produced by the VB.NET app and
/// writes equivalent JSON settings via <see cref="ISettingsStore"/>. Once a file is migrated
/// it is renamed to <c>.xml.migrated</c> so the migration never runs twice.
/// </summary>
public sealed class XmlSettingsMigrator
{
    private const string MigratedSuffix = ".migrated";

    private readonly string _legacySettingsDirectory;
    private readonly ISettingsStore _settingsStore;

    public XmlSettingsMigrator(string legacySettingsDirectory, ISettingsStore settingsStore)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legacySettingsDirectory);
        ArgumentNullException.ThrowIfNull(settingsStore);
        _legacySettingsDirectory = legacySettingsDirectory;
        _settingsStore = settingsStore;
    }

    /// <summary>Migrates all known XML settings files that have not yet been migrated.</summary>
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await MigrateApplicationSettingsAsync(cancellationToken);
        await MigrateShoppingListSettingsAsync(cancellationToken);
    }

    private async Task MigrateApplicationSettingsAsync(CancellationToken cancellationToken)
    {
        string xmlPath = Path.Combine(_legacySettingsDirectory, "ApplicationSettings.xml");
        if (!File.Exists(xmlPath) || File.Exists(xmlPath + MigratedSuffix))
        {
            return;
        }

        Dictionary<string, string> values = ParseXmlSettings(xmlPath);

        ApplicationSettingsModel model = new()
        {
            CheckForUpdatesOnStart = ParseBool(values, "CheckforUpdatesonStart", defaultVal: true),
            DataExportFormat = ParseString(values, "DataExportFormat", "Default"),
            AllowSkillOverride = ParseBool(values, "AllowSkillOverride", defaultVal: false),
            ShowToolTips = ParseBool(values, "ShowToolTips", defaultVal: true),
            RefiningImplantValue = ParseDouble(values, "RefiningImplantValue", 0),
            ManufacturingImplantValue = ParseDouble(values, "ManufacturingImplantValue", 0),
            CopyImplantValue = ParseDouble(values, "CopyImplantValue", 0),
            LoadAssetsOnStartup = ParseBool(values, "LoadAssetsonStartup", defaultVal: true),
            LoadBpsOnStartup = ParseBool(values, "LoadbpsonStartup", defaultVal: true),
            LoadEsiMarketDataOnStartup = ParseBool(values, "LoadESIMarketDataonStartup", defaultVal: true),
            LoadEsiSystemCostIndicesOnStartup = ParseBool(values, "LoadESISystemCostIndiciesDataonStartup", defaultVal: true),
            LoadEsiPublicStructuresOnStartup = ParseBool(values, "LoadESIPublicStructuresonStartup", defaultVal: true),
            SuppressEsiStatusMessages = ParseBool(values, "SupressESIStatusMessages", defaultVal: false),
            DisableSound = ParseBool(values, "DisableSound", defaultVal: false),
            IncludeInGameLinksInCopyText = ParseBool(values, "IncludeInGameLinksinCopyText", defaultVal: false),
            SaveFacilitiesByChar = ParseBool(values, "SaveFacilitiesbyChar", defaultVal: true),
            LoadBpsByChar = ParseBool(values, "LoadBPsbyChar", defaultVal: true),
            BrokerCorpStanding = ParseDouble(values, "BrokerCorpStanding", 5.0),
            BrokerFactionStanding = ParseDouble(values, "BrokerFactionStanding", 5.0),
            BaseSalesTaxRate = ParseDouble(values, "BaseSalesTaxRate", 4.5),
            BaseBrokerFeeRate = ParseDouble(values, "BaseBrokerFeeRate", 3.0),
            SccBrokerFeeSurcharge = ParseDouble(values, "SCCBrokerFeeSurcharge", 0.005),
            SccIndustryFeeSurcharge = ParseDouble(values, "SCCIndustryFeeSurcharge", 0.04),
            StructureTaxRate = ParseDouble(values, "StructureTaxRate", 0.0),
            DefaultBpMe = ParseInt(values, "DefaultBPME", 0),
            DefaultBpTe = ParseInt(values, "DefaultBPTE", 0),
            CheckBuildBuy = ParseBool(values, "CheckBuildBuy", defaultVal: false),
            DisableSvr = ParseBool(values, "DisableSVR", defaultVal: false),
            DisableGaTracking = ParseBool(values, "DisableGATracking", defaultVal: false),
            ShopListIncludeInventMats = ParseBool(values, "ShopListIncludeInventMats", defaultVal: true),
            ShopListIncludeCopyMats = ParseBool(values, "ShopListIncludeCopyMats", defaultVal: true),
            UpdatePricesRefreshInterval = ParseInt(values, "UpdatePricesRefreshInterval", 10),
            IgnoreSvrThresholdValue = ParseDouble(values, "IgnoreSVRThresholdValue", 0),
            SvrAveragePriceRegion = ParseString(values, "SVRAveragePriceRegion", "The Forge"),
            SvrAveragePriceDuration = ParseString(values, "SVRAveragePriceDuration", "7"),
            AutoUpdateSvrOnBpTab = ParseBool(values, "AutoUpdateSVRonBPTab", defaultVal: true),
            ProxyAddress = ParseString(values, "ProxyAddress", ""),
            ProxyPort = ParseInt(values, "ProxyPort", 0),
        };

        await _settingsStore.WriteAsync(model, cancellationToken);
        File.Move(xmlPath, xmlPath + MigratedSuffix, overwrite: true);
    }

    private async Task MigrateShoppingListSettingsAsync(CancellationToken cancellationToken)
    {
        string xmlPath = Path.Combine(_legacySettingsDirectory, "ShoppingListSettings.xml");
        if (!File.Exists(xmlPath) || File.Exists(xmlPath + MigratedSuffix))
        {
            return;
        }

        Dictionary<string, string> values = ParseXmlSettings(xmlPath);

        ShoppingListSettingsModel model = new()
        {
            DataExportFormat = ParseString(values, "DataExportFormat", "Default"),
            AlwaysOnTop = ParseBool(values, "AlwaysonTop", defaultVal: false),
            UpdateAssetsWhenUsed = ParseBool(values, "UpdateAssetsWhenUsed", defaultVal: false),
            Fees = ParseBool(values, "Fees", defaultVal: false),
            CalcBuyBuyOrder = ParseInt(values, "CalcBuyBuyOrder", 0),
            Usage = ParseBool(values, "Usage", defaultVal: false),
            ReloadBpsFromFile = ParseBool(values, "ReloadBPsFromFile", defaultVal: false),
        };

        await _settingsStore.WriteAsync(model, cancellationToken);
        File.Move(xmlPath, xmlPath + MigratedSuffix, overwrite: true);
    }

    private static Dictionary<string, string> ParseXmlSettings(string xmlPath)
    {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
        XmlDocument doc = new();
        doc.Load(xmlPath);
        XmlNodeList? nodes = doc.SelectNodes("//setting");
        if (nodes is null)
        {
            return result;
        }

        foreach (XmlNode node in nodes)
        {
            string? name = node.Attributes?["name"]?.Value;
            if (!string.IsNullOrEmpty(name))
            {
                result[name] = node.InnerText;
            }
        }

        return result;
    }

    private static bool ParseBool(Dictionary<string, string> d, string key, bool defaultVal) =>
        d.TryGetValue(key, out string? v) && bool.TryParse(v, out bool parsed) ? parsed : defaultVal;

    private static double ParseDouble(Dictionary<string, string> d, string key, double defaultVal) =>
        d.TryGetValue(key, out string? v) && double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed)
            ? parsed : defaultVal;

    private static int ParseInt(Dictionary<string, string> d, string key, int defaultVal) =>
        d.TryGetValue(key, out string? v) && int.TryParse(v, out int parsed) ? parsed : defaultVal;

    private static string ParseString(Dictionary<string, string> d, string key, string defaultVal) =>
        d.TryGetValue(key, out string? v) ? v : defaultVal;
}
