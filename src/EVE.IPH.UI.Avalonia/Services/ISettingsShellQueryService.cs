using EVE.IPH.Infrastructure.Settings.Models;

namespace EVE.IPH.UI.Avalonia.Services;

public interface ISettingsShellQueryService
{
    Task<SettingsShellScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default);
}

public sealed record SettingsShellScreenData(
    string DatabasePath,
    string SettingsStatusText,
    string OnboardingStatusText,
    string UpdateStatusText,
    long SupportedStaticDataBuild,
    string ImportedStaticDataBuildText,
    string StaticDataSourceUrl,
    string StaticDataImportedAtText,
    bool CheckForUpdatesOnStart,
    bool LoadAssetsOnStartup,
    bool LoadBlueprintsOnStartup,
    bool LoadMarketDataOnStartup,
    bool LoadSystemCostIndicesOnStartup,
    bool LoadPublicStructuresOnStartup,
    string StartupPreferencesStatusText,
    ApplicationSettingsModel CurrentSettings);