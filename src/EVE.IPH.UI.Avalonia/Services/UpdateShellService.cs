using Velopack;
using Velopack.Locators;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class UpdateShellService : IUpdateShellService
{
    private const string PrimaryFeedEnvironmentVariable = "EVEIPH_UPDATE_FEED";
    private const string SecondaryFeedEnvironmentVariable = "EVE_IPH_UPDATE_FEED";

    public UpdateShellStatus GetCurrentStatus()
    {
        if (!VelopackLocator.IsCurrentSet)
        {
            return new UpdateShellStatus(
                "This session is running outside a packaged Velopack install, so shell-driven update checks are unavailable here. Validate update distribution from a packaged build once the release feed is in place.",
                CanCheckForUpdates: false,
                CanApplyPreparedUpdate: false);
        }

        string? updateFeed = GetConfiguredUpdateFeed();
        if (string.IsNullOrWhiteSpace(updateFeed))
        {
            return new UpdateShellStatus(
                $"Update distribution is not configured for this packaged build yet. Set {PrimaryFeedEnvironmentVariable} (or {SecondaryFeedEnvironmentVariable}) to a Velopack release feed before using the in-app update flow.",
                CanCheckForUpdates: false,
                CanApplyPreparedUpdate: false);
        }

        UpdateManager updateManager = CreateManager(updateFeed);
        if (updateManager.UpdatePendingRestart is { } pendingRelease)
        {
            return CreatePendingRestartStatus(pendingRelease);
        }

        string currentVersion = updateManager.CurrentVersion?.ToString() ?? "unknown";
        return new UpdateShellStatus(
            $"Shell-driven update checks are available for this packaged install. Current version: {currentVersion}. Use the action below to query the configured release feed.",
            CanCheckForUpdates: true,
            CanApplyPreparedUpdate: false);
    }

    public async Task<UpdateShellStatus> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        UpdateShellStatus currentStatus = GetCurrentStatus();
        if (!currentStatus.CanCheckForUpdates && !currentStatus.CanApplyPreparedUpdate)
        {
            return currentStatus;
        }

        string? updateFeed = GetConfiguredUpdateFeed();
        if (string.IsNullOrWhiteSpace(updateFeed))
        {
            return currentStatus;
        }

        try
        {
            UpdateManager updateManager = CreateManager(updateFeed);
            if (updateManager.UpdatePendingRestart is { } pendingRelease)
            {
                return CreatePendingRestartStatus(pendingRelease);
            }

            UpdateInfo? updateInfo = await updateManager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (updateInfo is null)
            {
                string currentVersion = updateManager.CurrentVersion?.ToString() ?? "unknown";
                return new UpdateShellStatus(
                    $"No update is currently available from the configured release feed. Current version: {currentVersion}.",
                    CanCheckForUpdates: true,
                    CanApplyPreparedUpdate: false);
            }

            await updateManager.DownloadUpdatesAsync(updateInfo, _ => { }, cancellationToken).ConfigureAwait(false);

            VelopackAsset downloadedRelease = updateManager.UpdatePendingRestart ?? updateInfo.TargetFullRelease;
            return CreatePendingRestartStatus(downloadedRelease);
        }
        catch (Exception ex)
        {
            return new UpdateShellStatus(
                $"Unable to complete the update check: {ex.Message}",
                CanCheckForUpdates: true,
                CanApplyPreparedUpdate: false);
        }
    }

    public UpdateShellStatus ApplyPreparedUpdateAndRestart()
    {
        UpdateShellStatus currentStatus = GetCurrentStatus();
        if (!currentStatus.CanApplyPreparedUpdate)
        {
            return currentStatus;
        }

        string? updateFeed = GetConfiguredUpdateFeed();
        if (string.IsNullOrWhiteSpace(updateFeed))
        {
            return currentStatus;
        }

        try
        {
            UpdateManager updateManager = CreateManager(updateFeed);
            VelopackAsset? pendingRelease = updateManager.UpdatePendingRestart;
            if (pendingRelease is null)
            {
                return GetCurrentStatus();
            }

            updateManager.ApplyUpdatesAndRestart(pendingRelease, []);
            return new UpdateShellStatus(
                $"Applying update {pendingRelease.Version} and restarting the application...",
                CanCheckForUpdates: false,
                CanApplyPreparedUpdate: false);
        }
        catch (Exception ex)
        {
            return new UpdateShellStatus(
                $"Unable to apply the downloaded update automatically: {ex.Message}",
                CanCheckForUpdates: true,
                CanApplyPreparedUpdate: true);
        }
    }

    private static UpdateManager CreateManager(string updateFeed)
    {
        return new UpdateManager(updateFeed, new UpdateOptions(), VelopackLocator.Current);
    }

    private static UpdateShellStatus CreatePendingRestartStatus(VelopackAsset pendingRelease)
    {
        return new UpdateShellStatus(
            $"Update {pendingRelease.Version} has been downloaded and is ready to apply. Restart from the shell to finish installing it.",
            CanCheckForUpdates: true,
            CanApplyPreparedUpdate: true);
    }

    private static string? GetConfiguredUpdateFeed()
    {
        string? updateFeed = Environment.GetEnvironmentVariable(PrimaryFeedEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(updateFeed))
        {
            return updateFeed;
        }

        return Environment.GetEnvironmentVariable(SecondaryFeedEnvironmentVariable);
    }
}