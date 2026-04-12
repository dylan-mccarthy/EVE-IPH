using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Settings.Models;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class SettingsShellCommandService(
    ISettingsStore settingsStore,
    ISettingsShellQueryService queryService) : ISettingsShellCommandService
{
    private readonly ISettingsStore _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
    private readonly ISettingsShellQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));

    public async Task<Result<SettingsShellScreenData>> SaveStartupPreferencesAsync(SettingsShellStartupPreferencesRequest request, CancellationToken cancellationToken = default)
    {
        Maybe<ApplicationSettingsModel> existingResult = await _settingsStore
            .ReadAsync<ApplicationSettingsModel>(cancellationToken)
            .ConfigureAwait(false);

        ApplicationSettingsModel updatedSettings = (existingResult.HasValue ? existingResult.Value : new ApplicationSettingsModel()) with
        {
            CheckForUpdatesOnStart = request.CheckForUpdatesOnStart,
            LoadAssetsOnStartup = request.LoadAssetsOnStartup,
            LoadBpsOnStartup = request.LoadBlueprintsOnStartup,
            LoadEsiMarketDataOnStartup = request.LoadMarketDataOnStartup,
            LoadEsiSystemCostIndicesOnStartup = request.LoadSystemCostIndicesOnStartup,
            LoadEsiPublicStructuresOnStartup = request.LoadPublicStructuresOnStartup,
        };

        Result<bool> writeResult = await _settingsStore
            .WriteAsync(updatedSettings, cancellationToken)
            .ConfigureAwait(false);
        if (writeResult.IsFailure)
        {
            return Result<SettingsShellScreenData>.Failure(writeResult.Error);
        }

        SettingsShellScreenData screenData = await _queryService.GetScreenDataAsync(cancellationToken).ConfigureAwait(false);
        return Result<SettingsShellScreenData>.Success(screenData);
    }
}