using Avalonia.Controls;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IShellDialogService
{
    Task<bool> ConfirmLegacyDatabaseImportAsync(Window owner, string sourcePath, string destinationPath);

    Task ShowLegacyDatabaseImportCompleteAsync(Window owner, LegacyDatabaseImportScreenResult result);

    Task ShowRestartFailureAsync(Window owner, string errorMessage);

    Task ShowFirstRunOnboardingAsync(Window owner);

    Task ShowUpdatePlaceholderAsync(Window owner);
}