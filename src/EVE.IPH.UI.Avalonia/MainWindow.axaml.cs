using Avalonia.Platform.Storage;
using EVE.IPH.UI.Avalonia.Services;
using EVE.IPH.UI.Avalonia.ViewModels;

using Avalonia.Controls;

namespace EVE.IPH.UI.Avalonia;

public partial class MainWindow : Window
{
    private readonly IShellDialogService? _shellDialogService;
    private readonly MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel, IShellDialogService shellDialogService)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _shellDialogService = shellDialogService ?? throw new ArgumentNullException(nameof(shellDialogService));

        InitializeComponent();
        DataContext = _viewModel;
    }

    private async void BrowseLegacyDatabaseImport_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Legacy EVE IPH Database",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("SQLite Databases")
                {
                    Patterns = ["*.sqlite", "*.db"],
                    MimeTypes = ["application/vnd.sqlite3"],
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = ["*"],
                },
            ],
        });

        IStorageFile? selectedFile = files.FirstOrDefault();
        if (selectedFile is null)
        {
            return;
        }

        string? localPath = selectedFile.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(localPath) || _viewModel is null)
        {
            return;
        }

        if (!await ConfirmOverwriteIfNeededAsync(localPath))
        {
            return;
        }

        LegacyDatabaseImportScreenResult? result = await _viewModel.ImportLegacyDatabaseFromPathAsync(localPath);
        if (result is not null)
        {
            await ShowImportCompleteDialogAsync(result);
        }
    }

    private async void ImportDetectedLegacyDatabase_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel is null || string.IsNullOrWhiteSpace(_viewModel.LegacyImportSourcePath))
        {
            return;
        }

        if (!await ConfirmOverwriteIfNeededAsync(_viewModel.LegacyImportSourcePath))
        {
            return;
        }

        LegacyDatabaseImportScreenResult? result = await _viewModel.ImportDetectedLegacyDatabaseAsync();
        if (result is not null)
        {
            await ShowImportCompleteDialogAsync(result);
        }
    }

    private async Task<bool> ConfirmOverwriteIfNeededAsync(string sourcePath)
    {
        if (_viewModel is null || _shellDialogService is null)
        {
            return false;
        }

        if (!_viewModel.ShouldConfirmLegacyDatabaseImport(sourcePath))
        {
            return true;
        }

        string destinationPath = Path.GetFullPath(EVE.IPH.Infrastructure.Settings.Storage.AppDatabasePath.GetCanonicalDatabasePath());

        return await _shellDialogService.ConfirmLegacyDatabaseImportAsync(this, sourcePath, destinationPath);
    }

    private async Task ShowImportCompleteDialogAsync(LegacyDatabaseImportScreenResult result)
    {
        if (_shellDialogService is null)
        {
            return;
        }

        await _shellDialogService.ShowLegacyDatabaseImportCompleteAsync(this, result);
    }

    private async void RestartApplication_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        string? errorMessage = _viewModel.TryRestartApplication();
        if (!string.IsNullOrWhiteSpace(errorMessage) && _shellDialogService is not null)
        {
            await _shellDialogService.ShowRestartFailureAsync(this, errorMessage);
        }
    }

    private async void ShowFirstRunOnboarding_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_shellDialogService is null)
        {
            return;
        }

        await _shellDialogService.ShowFirstRunOnboardingAsync(this);
    }

    private async void CheckForUpdates_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.CheckForUpdatesAsync();
    }

    private void ApplyPreparedUpdate_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.ApplyPreparedUpdateAndRestart();
    }

    private async void SaveStartupPreferences_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.SaveStartupPreferencesAsync();
    }
}