using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVE.IPH.Infrastructure.Settings.Models;
using EVE.IPH.Infrastructure.Settings.Storage;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IApplicationRestartService _applicationRestartService;
    private readonly ILegacyDatabaseImportService _legacyDatabaseImportService;
    private readonly StaticDataSettingsModel _staticDataSettings;
    private string _legacyImportStatus = string.Empty;
    private string? _legacyImportSourcePath;
    private bool _restartRequired;

    public MainWindowViewModel(
        CharacterManagementViewModel characterManagement,
        BlueprintManagementViewModel blueprints,
        ManufacturingWorkspaceViewModel manufacturing,
        StructureFacilityManagementViewModel structureFacilities,
        AssetsViewModel assets,
        IndustryJobsViewModel industryJobs,
        ResearchAgentsViewModel researchAgents,
        ILegacyDatabaseImportService legacyDatabaseImportService,
        IApplicationRestartService applicationRestartService,
        StaticDataSettingsModel? staticDataSettings = null)
    {
        CharacterManagement = characterManagement ?? throw new ArgumentNullException(nameof(characterManagement));
        Blueprints = blueprints ?? throw new ArgumentNullException(nameof(blueprints));
        Manufacturing = manufacturing ?? throw new ArgumentNullException(nameof(manufacturing));
        StructureFacilities = structureFacilities ?? throw new ArgumentNullException(nameof(structureFacilities));
        Assets = assets ?? throw new ArgumentNullException(nameof(assets));
        IndustryJobs = industryJobs ?? throw new ArgumentNullException(nameof(industryJobs));
        ResearchAgents = researchAgents ?? throw new ArgumentNullException(nameof(researchAgents));
        _legacyDatabaseImportService = legacyDatabaseImportService ?? throw new ArgumentNullException(nameof(legacyDatabaseImportService));
        _applicationRestartService = applicationRestartService ?? throw new ArgumentNullException(nameof(applicationRestartService));
        _staticDataSettings = staticDataSettings ?? new StaticDataSettingsModel();

        _legacyImportSourcePath = _legacyDatabaseImportService.GetDetectedLegacyDatabasePath();
        if (!string.IsNullOrWhiteSpace(_legacyImportSourcePath))
        {
            _legacyImportStatus = "Legacy database detected. Import it into the new app-data store when you're ready; the current session will still require a restart to reload imported data.";
        }
        else
        {
            _legacyImportStatus = "No legacy database was detected automatically. You can still browse to an older SQLite database and import it manually.";
        }

    }

    public string Title => "EVE IPH Modern";

    public string Subtitle => "Phase 11 host with DI-backed shell services over the extracted domains, now including blueprint management, manufacturing, and the first structure/facility-management workflows.";

    public string DatabasePath => Path.GetFullPath(AppDatabasePath.GetCanonicalDatabasePath());

    public string SettingsStatusText => "Import, onboarding, and update entry points are now routed through the Avalonia shell rather than ad hoc startup behavior.";

    public string OnboardingStatusText => "The first-run onboarding dialog path is available for shell-driven prompts as Phase 11 expands.";

    public string UpdateStatusText => "Velopack bootstrap is in place. Update prompts should surface through the shell dialog service once check/apply flows are wired.";

    public long SupportedStaticDataBuild => _staticDataSettings.SupportedBuildNumber;

    public string ImportedStaticDataBuildText => _staticDataSettings.ImportedBuildNumber?.ToString() ?? "Not imported yet";

    public string StaticDataSourceUrl => _staticDataSettings.SourceArchiveUrl;

    public string StaticDataImportedAtText => _staticDataSettings.ImportedAtUtc?.ToString("u") ?? "No import recorded yet";

    public bool CanBrowseLegacyDatabase => !RestartRequired;

    public bool CanImportLegacyDatabase => !RestartRequired && !string.IsNullOrWhiteSpace(_legacyImportSourcePath);

    public string LegacyImportButtonText => CanImportLegacyDatabase ? "Import Legacy DB" : "No Legacy DB Detected";

    public bool RestartRequired
    {
        get => _restartRequired;
        private set
        {
            if (SetProperty(ref _restartRequired, value))
            {
                OnPropertyChanged(nameof(CanBrowseLegacyDatabase));
                OnPropertyChanged(nameof(CanImportLegacyDatabase));
                OnPropertyChanged(nameof(LegacyImportButtonText));
            }
        }
    }

    public string LegacyImportStatus
    {
        get => _legacyImportStatus;
        private set => SetProperty(ref _legacyImportStatus, value);
    }

    public string? LegacyImportSourcePath
    {
        get => _legacyImportSourcePath;
        private set
        {
            if (SetProperty(ref _legacyImportSourcePath, value))
            {
                OnPropertyChanged(nameof(CanImportLegacyDatabase));
                OnPropertyChanged(nameof(LegacyImportButtonText));
            }
        }
    }

    public AssetsViewModel Assets { get; }

    public BlueprintManagementViewModel Blueprints { get; }

    public CharacterManagementViewModel CharacterManagement { get; }

    public ManufacturingWorkspaceViewModel Manufacturing { get; }

    public StructureFacilityManagementViewModel StructureFacilities { get; }

    public IndustryJobsViewModel IndustryJobs { get; }

    public ResearchAgentsViewModel ResearchAgents { get; }

    public bool ShouldConfirmLegacyDatabaseImport(string sourcePath)
    {
        return _legacyDatabaseImportService.ImportWouldOverwrite(sourcePath);
    }

    public async Task<LegacyDatabaseImportScreenResult?> ImportLegacyDatabaseFromPathAsync(string sourcePath)
    {
        return await ImportLegacyDatabaseCoreAsync(() => _legacyDatabaseImportService.ImportAsync(sourcePath)).ConfigureAwait(false);
    }

    public async Task<LegacyDatabaseImportScreenResult?> ImportDetectedLegacyDatabaseAsync()
    {
        return await ImportLegacyDatabaseCoreAsync(() => _legacyDatabaseImportService.ImportDetectedAsync()).ConfigureAwait(false);
    }

    private async Task<LegacyDatabaseImportScreenResult?> ImportLegacyDatabaseCoreAsync(Func<Task<LegacyDatabaseImportScreenResult>> importOperation)
    {
        try
        {
            LegacyDatabaseImportScreenResult result = await importOperation().ConfigureAwait(false);

            string backupMessage = string.IsNullOrWhiteSpace(result.BackupPath)
                ? string.Empty
                : $" A backup of the current database was written to {result.BackupPath}.";

            LegacyImportStatus = $"Imported legacy database from {result.SourcePath} to {result.DestinationPath}.{backupMessage} Restart the app now to reload the imported data into the current screens.";
            LegacyImportSourcePath = null;
            RestartRequired = true;
            return result;
        }
        catch (Exception ex)
        {
            LegacyImportStatus = $"Legacy database import failed: {ex.Message}";
            return null;
        }
    }

    public string? TryRestartApplication()
    {
        try
        {
            _applicationRestartService.Restart();
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}