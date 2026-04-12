using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public interface ISettingsShellCommandService
{
    Task<Result<SettingsShellScreenData>> SaveStartupPreferencesAsync(SettingsShellStartupPreferencesRequest request, CancellationToken cancellationToken = default);
}

public sealed record SettingsShellStartupPreferencesRequest(
    bool CheckForUpdatesOnStart,
    bool LoadAssetsOnStartup,
    bool LoadBlueprintsOnStartup,
    bool LoadMarketDataOnStartup,
    bool LoadSystemCostIndicesOnStartup,
    bool LoadPublicStructuresOnStartup);