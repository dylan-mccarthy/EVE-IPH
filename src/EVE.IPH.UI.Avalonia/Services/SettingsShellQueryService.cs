using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Infrastructure.Settings.Models;
using EVE.IPH.Infrastructure.Settings.Storage;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class SettingsShellQueryService(ISettingsStore settingsStore) : ISettingsShellQueryService
{
    private readonly ISettingsStore _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));

    public async Task<SettingsShellScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default)
    {
        Maybe<ApplicationSettingsModel> applicationSettingsResult = await _settingsStore
            .ReadAsync<ApplicationSettingsModel>(cancellationToken)
            .ConfigureAwait(false);
        Maybe<StaticDataSettingsModel> staticDataSettingsResult = await _settingsStore
            .ReadAsync<StaticDataSettingsModel>(cancellationToken)
            .ConfigureAwait(false);

        ApplicationSettingsModel applicationSettings = applicationSettingsResult.GetValueOrDefault(new ApplicationSettingsModel());
        StaticDataSettingsModel staticDataSettings = staticDataSettingsResult.GetValueOrDefault(new StaticDataSettingsModel());

        string onboardingStatusText = applicationSettings.LoadAssetsOnStartup || applicationSettings.LoadBpsOnStartup || applicationSettings.LoadEsiMarketDataOnStartup
            ? "Startup data loading is configured from the persisted application settings below. The onboarding dialog remains available from the shell for first-run guidance."
            : "Most startup loading is currently disabled in persisted settings. Use the startup preferences below to decide what should preload before the shell fills out more of the first-run experience.";

        string updateStatusText = applicationSettings.CheckForUpdatesOnStart
            ? "Automatic update checks on startup remain enabled in persisted settings. The shell now owns explicit check/apply actions, while release-feed configuration still belongs to packaging and distribution."
            : "Automatic update checks on startup are disabled in persisted settings. The shell still exposes a manual check/apply path when packaging provides a release feed.";

        int enabledStartupOptions = CountEnabledStartupOptions(applicationSettings);
        string startupPreferencesStatusText = $"{enabledStartupOptions} startup preference{(enabledStartupOptions == 1 ? string.Empty : "s")} currently enabled. Save changes here to persist how much shell data should preload on startup.";

        return new SettingsShellScreenData(
            Path.GetFullPath(AppDatabasePath.GetCanonicalDatabasePath()),
            "Database import, startup loading preferences, and update-check behavior are now backed by persisted shell settings instead of placeholder status text.",
            onboardingStatusText,
            updateStatusText,
            staticDataSettings.SupportedBuildNumber,
            staticDataSettings.ImportedBuildNumber?.ToString() ?? "Not imported yet",
            staticDataSettings.SourceArchiveUrl,
            staticDataSettings.ImportedAtUtc?.ToString("u") ?? "No import recorded yet",
            applicationSettings.CheckForUpdatesOnStart,
            applicationSettings.LoadAssetsOnStartup,
            applicationSettings.LoadBpsOnStartup,
            applicationSettings.LoadEsiMarketDataOnStartup,
            applicationSettings.LoadEsiSystemCostIndicesOnStartup,
            applicationSettings.LoadEsiPublicStructuresOnStartup,
            startupPreferencesStatusText,
            applicationSettings);
    }

    private static int CountEnabledStartupOptions(ApplicationSettingsModel settings)
    {
        int count = 0;
        count += settings.CheckForUpdatesOnStart ? 1 : 0;
        count += settings.LoadAssetsOnStartup ? 1 : 0;
        count += settings.LoadBpsOnStartup ? 1 : 0;
        count += settings.LoadEsiMarketDataOnStartup ? 1 : 0;
        count += settings.LoadEsiSystemCostIndicesOnStartup ? 1 : 0;
        count += settings.LoadEsiPublicStructuresOnStartup ? 1 : 0;
        return count;
    }
}