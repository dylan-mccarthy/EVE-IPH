using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Settings.Models;
using EVE.IPH.Infrastructure.Settings.Storage;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IApplicationRestartService _applicationRestartService;
    private readonly ILegacyDatabaseImportService _legacyDatabaseImportService;
    private readonly ISettingsShellQueryService _settingsShellQueryService;
    private readonly ISettingsShellCommandService _settingsShellCommandService;
    private readonly IUpdateShellService _updateShellService;
    private string _legacyImportStatus = string.Empty;
    private string? _legacyImportSourcePath;
    private bool _restartRequired;
    private string _databasePath = string.Empty;
    private string _settingsStatusText = "Loading settings shell state...";
    private string _onboardingStatusText = "Loading settings shell state...";
    private string _updateStatusText = "Loading settings shell state...";
    private long _supportedStaticDataBuild;
    private string _importedStaticDataBuildText = "Not imported yet";
    private string _staticDataSourceUrl = string.Empty;
    private string _staticDataImportedAtText = "No import recorded yet";
    private bool _checkForUpdatesOnStart;
    private bool _loadAssetsOnStartup;
    private bool _loadBlueprintsOnStartup;
    private bool _loadMarketDataOnStartup;
    private bool _loadSystemCostIndicesOnStartup;
    private bool _loadPublicStructuresOnStartup;
    private string _startupPreferencesStatusText = "Loading settings shell state...";
    private bool _isSavingSettings;
    private bool _isCheckingForUpdates;
    private bool _updateChecksAvailable;
    private bool _preparedUpdateReady;

    public MainWindowViewModel(
        CharacterManagementViewModel characterManagement,
        BlueprintManagementViewModel blueprints,
        ManufacturingWorkspaceViewModel manufacturing,
        MarketPriceViewModel market,
        ShoppingListViewModel shoppingList,
        MiningReprocessingViewModel miningReprocessing,
        StructureFacilityManagementViewModel structureFacilities,
        AssetsViewModel assets,
        IndustryJobsViewModel industryJobs,
        ResearchAgentsViewModel researchAgents,
        ILegacyDatabaseImportService legacyDatabaseImportService,
        IApplicationRestartService applicationRestartService,
        ISettingsShellQueryService settingsShellQueryService,
        ISettingsShellCommandService settingsShellCommandService,
        IUpdateShellService updateShellService)
    {
        CharacterManagement = characterManagement ?? throw new ArgumentNullException(nameof(characterManagement));
        Blueprints = blueprints ?? throw new ArgumentNullException(nameof(blueprints));
        Manufacturing = manufacturing ?? throw new ArgumentNullException(nameof(manufacturing));
        Market = market ?? throw new ArgumentNullException(nameof(market));
        ShoppingList = shoppingList ?? throw new ArgumentNullException(nameof(shoppingList));
        MiningReprocessing = miningReprocessing ?? throw new ArgumentNullException(nameof(miningReprocessing));
        StructureFacilities = structureFacilities ?? throw new ArgumentNullException(nameof(structureFacilities));
        Assets = assets ?? throw new ArgumentNullException(nameof(assets));
        IndustryJobs = industryJobs ?? throw new ArgumentNullException(nameof(industryJobs));
        ResearchAgents = researchAgents ?? throw new ArgumentNullException(nameof(researchAgents));
        _legacyDatabaseImportService = legacyDatabaseImportService ?? throw new ArgumentNullException(nameof(legacyDatabaseImportService));
        _applicationRestartService = applicationRestartService ?? throw new ArgumentNullException(nameof(applicationRestartService));
        _settingsShellQueryService = settingsShellQueryService ?? throw new ArgumentNullException(nameof(settingsShellQueryService));
        _settingsShellCommandService = settingsShellCommandService ?? throw new ArgumentNullException(nameof(settingsShellCommandService));
        _updateShellService = updateShellService ?? throw new ArgumentNullException(nameof(updateShellService));

        _legacyImportSourcePath = _legacyDatabaseImportService.GetDetectedLegacyDatabasePath();
        if (!string.IsNullOrWhiteSpace(_legacyImportSourcePath))
        {
            _legacyImportStatus = "Legacy database detected. Import it into the new app-data store when you're ready; the current session will still require a restart to reload imported data.";
        }
        else
        {
            _legacyImportStatus = "No legacy database was detected automatically. You can still browse to an older SQLite database and import it manually.";
        }

        LoadTask = LoadSettingsShellAsync();
    }

    public Task LoadTask { get; }

    public string Title => "EVE IPH Modern";

    public string Subtitle => "Phase 11 host with DI-backed shell services over the extracted domains, now including blueprint management, manufacturing, the first market/update workspace, shopping-list workflows, the first mining/reprocessing slice, and structure/facility-management workflows.";

    public string DatabasePath
    {
        get => _databasePath;
        private set => SetProperty(ref _databasePath, value);
    }

    public string SettingsStatusText
    {
        get => _settingsStatusText;
        private set => SetProperty(ref _settingsStatusText, value);
    }

    public string OnboardingStatusText
    {
        get => _onboardingStatusText;
        private set => SetProperty(ref _onboardingStatusText, value);
    }

    public string UpdateStatusText
    {
        get => _updateStatusText;
        private set => SetProperty(ref _updateStatusText, value);
    }

    public long SupportedStaticDataBuild
    {
        get => _supportedStaticDataBuild;
        private set => SetProperty(ref _supportedStaticDataBuild, value);
    }

    public string ImportedStaticDataBuildText
    {
        get => _importedStaticDataBuildText;
        private set => SetProperty(ref _importedStaticDataBuildText, value);
    }

    public string StaticDataSourceUrl
    {
        get => _staticDataSourceUrl;
        private set => SetProperty(ref _staticDataSourceUrl, value);
    }

    public string StaticDataImportedAtText
    {
        get => _staticDataImportedAtText;
        private set => SetProperty(ref _staticDataImportedAtText, value);
    }

    public bool CheckForUpdatesOnStart
    {
        get => _checkForUpdatesOnStart;
        set => SetProperty(ref _checkForUpdatesOnStart, value);
    }

    public bool LoadAssetsOnStartup
    {
        get => _loadAssetsOnStartup;
        set => SetProperty(ref _loadAssetsOnStartup, value);
    }

    public bool LoadBlueprintsOnStartup
    {
        get => _loadBlueprintsOnStartup;
        set => SetProperty(ref _loadBlueprintsOnStartup, value);
    }

    public bool LoadMarketDataOnStartup
    {
        get => _loadMarketDataOnStartup;
        set => SetProperty(ref _loadMarketDataOnStartup, value);
    }

    public bool LoadSystemCostIndicesOnStartup
    {
        get => _loadSystemCostIndicesOnStartup;
        set => SetProperty(ref _loadSystemCostIndicesOnStartup, value);
    }

    public bool LoadPublicStructuresOnStartup
    {
        get => _loadPublicStructuresOnStartup;
        set => SetProperty(ref _loadPublicStructuresOnStartup, value);
    }

    public string StartupPreferencesStatusText
    {
        get => _startupPreferencesStatusText;
        private set => SetProperty(ref _startupPreferencesStatusText, value);
    }

    public bool IsSavingSettings
    {
        get => _isSavingSettings;
        private set
        {
            if (SetProperty(ref _isSavingSettings, value))
            {
                OnPropertyChanged(nameof(CanSaveStartupPreferences));
            }
        }
    }

    public bool CanSaveStartupPreferences => !IsSavingSettings;

    public bool CanCheckForUpdates => _updateChecksAvailable && !IsCheckingForUpdates;

    public bool CanApplyPreparedUpdate => _preparedUpdateReady && !IsCheckingForUpdates;

    public bool CanBrowseLegacyDatabase => !RestartRequired;

    public bool CanImportLegacyDatabase => !RestartRequired && !string.IsNullOrWhiteSpace(_legacyImportSourcePath);

    public string LegacyImportButtonText => CanImportLegacyDatabase ? "Import Legacy DB" : "No Legacy DB Detected";

    public bool IsCheckingForUpdates
    {
        get => _isCheckingForUpdates;
        private set
        {
            if (SetProperty(ref _isCheckingForUpdates, value))
            {
                OnPropertyChanged(nameof(CanCheckForUpdates));
                OnPropertyChanged(nameof(CanApplyPreparedUpdate));
            }
        }
    }

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

    public MarketPriceViewModel Market { get; }

    public ShoppingListViewModel ShoppingList { get; }

    public MiningReprocessingViewModel MiningReprocessing { get; }

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

    public async Task SaveStartupPreferencesAsync()
    {
        if (IsSavingSettings)
        {
            return;
        }

        try
        {
            IsSavingSettings = true;
            Result<SettingsShellScreenData> result = await _settingsShellCommandService
                .SaveStartupPreferencesAsync(new SettingsShellStartupPreferencesRequest(
                    CheckForUpdatesOnStart,
                    LoadAssetsOnStartup,
                    LoadBlueprintsOnStartup,
                    LoadMarketDataOnStartup,
                    LoadSystemCostIndicesOnStartup,
                    LoadPublicStructuresOnStartup))
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                StartupPreferencesStatusText = $"Unable to save startup preferences: {result.Error.Message}";
                return;
            }

            ApplySettingsShellData(result.Value);
        }
        catch (Exception ex)
        {
            StartupPreferencesStatusText = $"Unable to save startup preferences: {ex.Message}";
        }
        finally
        {
            IsSavingSettings = false;
        }
    }

    public async Task CheckForUpdatesAsync()
    {
        if (IsCheckingForUpdates)
        {
            return;
        }

        try
        {
            IsCheckingForUpdates = true;
            UpdateStatusText = "Checking the configured release feed for updates...";
            UpdateShellStatus status = await _updateShellService.CheckForUpdatesAsync().ConfigureAwait(false);
            ApplyUpdateShellStatus(status);
        }
        catch (Exception ex)
        {
            UpdateStatusText = $"Unable to complete the update check: {ex.Message}";
            _preparedUpdateReady = false;
            OnPropertyChanged(nameof(CanApplyPreparedUpdate));
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    public void ApplyPreparedUpdateAndRestart()
    {
        UpdateShellStatus status = _updateShellService.ApplyPreparedUpdateAndRestart();
        ApplyUpdateShellStatus(status);
    }

    private async Task LoadSettingsShellAsync()
    {
        SettingsShellScreenData screenData = await _settingsShellQueryService.GetScreenDataAsync().ConfigureAwait(false);
        ApplySettingsShellData(screenData);
        ApplyUpdateShellStatus(_updateShellService.GetCurrentStatus());
    }

    private void ApplySettingsShellData(SettingsShellScreenData screenData)
    {
        DatabasePath = screenData.DatabasePath;
        SettingsStatusText = screenData.SettingsStatusText;
        OnboardingStatusText = screenData.OnboardingStatusText;
        UpdateStatusText = screenData.UpdateStatusText;
        SupportedStaticDataBuild = screenData.SupportedStaticDataBuild;
        ImportedStaticDataBuildText = screenData.ImportedStaticDataBuildText;
        StaticDataSourceUrl = screenData.StaticDataSourceUrl;
        StaticDataImportedAtText = screenData.StaticDataImportedAtText;
        CheckForUpdatesOnStart = screenData.CheckForUpdatesOnStart;
        LoadAssetsOnStartup = screenData.LoadAssetsOnStartup;
        LoadBlueprintsOnStartup = screenData.LoadBlueprintsOnStartup;
        LoadMarketDataOnStartup = screenData.LoadMarketDataOnStartup;
        LoadSystemCostIndicesOnStartup = screenData.LoadSystemCostIndicesOnStartup;
        LoadPublicStructuresOnStartup = screenData.LoadPublicStructuresOnStartup;
        StartupPreferencesStatusText = screenData.StartupPreferencesStatusText;
    }

    private void ApplyUpdateShellStatus(UpdateShellStatus status)
    {
        UpdateStatusText = status.StatusText;
        _updateChecksAvailable = status.CanCheckForUpdates;
        _preparedUpdateReady = status.CanApplyPreparedUpdate;
        OnPropertyChanged(nameof(CanCheckForUpdates));
        OnPropertyChanged(nameof(CanApplyPreparedUpdate));
    }
}