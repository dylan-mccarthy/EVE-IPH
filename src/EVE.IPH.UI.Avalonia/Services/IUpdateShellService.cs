namespace EVE.IPH.UI.Avalonia.Services;

public interface IUpdateShellService
{
    UpdateShellStatus GetCurrentStatus();

    Task<UpdateShellStatus> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    UpdateShellStatus ApplyPreparedUpdateAndRestart();
}

public sealed record UpdateShellStatus(
    string StatusText,
    bool CanCheckForUpdates,
    bool CanApplyPreparedUpdate);