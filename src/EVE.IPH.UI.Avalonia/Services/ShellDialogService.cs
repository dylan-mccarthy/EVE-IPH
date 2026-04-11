using Avalonia.Controls;
using EVE.IPH.Infrastructure.Settings.Storage;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class ShellDialogService(IModalDialogService modalDialogService) : IShellDialogService
{
    private readonly IModalDialogService _modalDialogService = modalDialogService ?? throw new ArgumentNullException(nameof(modalDialogService));

    public Task<bool> ConfirmLegacyDatabaseImportAsync(Window owner, string sourcePath, string destinationPath)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        return _modalDialogService.ShowConfirmationAsync(owner, new DialogRequest(
            Title: "Replace Current Database?",
            Message: "Importing this database will replace the current app-data database.",
            Details:
            [
                $"Source: {sourcePath}",
                $"Destination: {destinationPath}",
                "The current database will be backed up before the import continues.",
            ],
            PrimaryButtonText: "Import and Replace",
            SecondaryButtonText: "Cancel"));
    }

    public Task ShowLegacyDatabaseImportCompleteAsync(Window owner, LegacyDatabaseImportScreenResult result)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(result);

        string backupText = string.IsNullOrWhiteSpace(result.BackupPath)
            ? "No backup was needed because there was no existing app-data database to replace."
            : $"Backup created: {result.BackupPath}";

        return _modalDialogService.ShowMessageAsync(owner, new DialogRequest(
            Title: "Import Complete",
            Message: "Legacy database import completed successfully.",
            Details:
            [
                $"Imported from: {result.SourcePath}",
                $"Active database: {result.DestinationPath}",
                backupText,
                "Restart the application now to reload the imported data into the current screens.",
            ],
            PrimaryButtonText: "Close"));
    }

    public Task ShowRestartFailureAsync(Window owner, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return _modalDialogService.ShowMessageAsync(owner, new DialogRequest(
            Title: "Restart Failed",
            Message: "The application could not restart automatically.",
            Details:
            [
                errorMessage,
                "Close and relaunch the application manually to continue with the imported data.",
            ],
            PrimaryButtonText: "Close"));
    }

    public Task ShowFirstRunOnboardingAsync(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        return _modalDialogService.ShowMessageAsync(owner, new DialogRequest(
            Title: "Welcome to EVE IPH Modern",
            Message: "This shell is the first Phase 11 Avalonia host for the modernized application.",
            Details:
            [
                $"Your active database lives in: {AppDatabasePath.GetCanonicalDatabasePath()}",
                "You can import a legacy SQLite database from the shell header if you want to bring forward existing data.",
                "Additional onboarding prompts can be routed through this shell dialog service as Phase 11 expands.",
            ],
            PrimaryButtonText: "Close"));
    }

    public Task ShowUpdatePlaceholderAsync(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        return _modalDialogService.ShowMessageAsync(owner, new DialogRequest(
            Title: "Updates",
            Message: "The shell dialog path for update prompts is ready.",
            Details:
            [
                "Velopack bootstrap is already in place.",
                "When update checks and apply flows are implemented, they should surface through this shell dialog service rather than bespoke window code.",
            ],
            PrimaryButtonText: "Close"));
    }
}