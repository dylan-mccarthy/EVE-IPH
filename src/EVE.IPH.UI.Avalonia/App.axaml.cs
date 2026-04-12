using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EVE.IPH.Domain.Assets.Services;
using EVE.IPH.Domain.Characters.Services;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Industry.Services;
using EVE.IPH.Domain.Manufacturing.Services;
using EVE.IPH.Domain.Market.Services;
using EVE.IPH.Domain.Reprocessing.Services;
using EVE.IPH.Domain.ShoppingList.Services;
using EVE.IPH.Infrastructure.Data.Connections;
using EVE.IPH.Infrastructure.Data.Repositories.App;
using EVE.IPH.Infrastructure.Data.Repositories.Sde;
using EVE.IPH.Infrastructure.ESI;
using EVE.IPH.Infrastructure.ESI.DependencyInjection;
using EVE.IPH.Infrastructure.Settings;
using EVE.IPH.Infrastructure.Settings.Models;
using EVE.IPH.Infrastructure.Settings.Storage;
using EVE.IPH.UI.Avalonia.Services;
using EVE.IPH.UI.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace EVE.IPH.UI.Avalonia;

public partial class App : Application
{
    private static readonly ServiceProvider Services = ConfigureServices();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider ConfigureServices()
    {
        ServiceCollection services = new();

        // The StartupOrchestrator in Program.cs ensures the database exists and all
        // migrations have run before this method is called.
        string databasePath = AppDatabasePath.GetCanonicalDatabasePath();
        string settingsDirectory = PlatformStoragePath.GetSettingsDirectory();
        JsonSettingsStore settingsStore = new(settingsDirectory);
        StaticDataSettingsModel staticDataSettings = settingsStore
            .ReadAsync<StaticDataSettingsModel>()
            .GetAwaiter()
            .GetResult()
            .GetValueOrDefault(new StaticDataSettingsModel());
        ApplicationSettingsModel applicationSettings = settingsStore
            .ReadAsync<ApplicationSettingsModel>()
            .GetAwaiter()
            .GetResult()
            .GetValueOrDefault(new ApplicationSettingsModel());

        services.AddEsiInfrastructure();

        services.AddSingleton<IAssetSnapshotHydrator, AssetSnapshotHydrator>();
        services.AddSingleton<IAssetViewFilterService, AssetViewFilterService>();
        services.AddSingleton<ICharacterAssetService, CharacterAssetService>();
        services.AddSingleton<ICorporationAssetService, CorporationAssetService>();
        services.AddSingleton<ICharacterService, CharacterService>();
        services.AddSingleton<ICharacterIndustryJobService, CharacterIndustryJobService>();
        services.AddSingleton<ICorporationIndustryJobService, CorporationIndustryJobService>();
        services.AddSingleton<ICorporationBlueprintService, CorporationBlueprintService>();
        services.AddSingleton<IIndustryJobService, IndustryJobService>();
        services.AddSingleton<IIndustryJobPresentationService, IndustryJobPresentationService>();
        services.AddSingleton<IResearchAgentDatacoreService, ResearchAgentDatacoreService>();
        services.AddSingleton<ICorporationCapabilityResolver, EsiCorporationCapabilityResolver>();
        services.AddSingleton(staticDataSettings);
        services.AddSingleton(applicationSettings);
        services.AddSingleton<ISettingsStore>(settingsStore);

        services.AddSingleton<IDbConnectionFactory>(_ => new SqliteConnectionFactory($"Data Source={databasePath}"));
        services.AddSingleton<ICharacterRepository, SqliteCharacterRepository>();
        services.AddSingleton<ICorporationConnectionRepository, SqliteCorporationConnectionRepository>();
        services.AddSingleton<IAssetRepository, SqliteAssetRepository>();
        services.AddSingleton<ICharacterSkillRepository, SqliteCharacterSkillRepository>();
        services.AddSingleton<ICharacterStandingRepository, SqliteCharacterStandingRepository>();
        services.AddSingleton<ICharacterResearchAgentRepository, SqliteCharacterResearchAgentRepository>();
        services.AddSingleton<IIndustryJobRepository, SqliteIndustryJobRepository>();
        services.AddSingleton<IIndustryFacilityRepository, SqliteIndustryFacilityRepository>();
        services.AddSingleton<IMarketCacheRepository, SqliteMarketCacheRepository>();
        services.AddSingleton<IShoppingListRepository, SqliteShoppingListRepository>();
        services.AddSingleton<IOwnedBlueprintRepository, SqliteOwnedBlueprintRepository>();
        services.AddSingleton<IAssetReadRepository, SqliteAssetReadRepository>();
        services.AddSingleton<IIndustryJobReadRepository, SqliteIndustryJobReadRepository>();
        services.AddSingleton<IOwnedBlueprintViewRepository, SqliteOwnedBlueprintReadRepository>();
        services.AddSingleton<IResearchAgentDefinitionRepository, SqliteResearchAgentDefinitionRepository>();
        services.AddSingleton<IItemRepository, SqliteItemRepository>();
        services.AddSingleton<IBlueprintRepository, SqliteBlueprintRepository>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IResearchAgentService, ResearchAgentService>();
        services.AddSingleton<IMarketPriceSourcePreferenceProvider, UpdatePriceSettingsMarketPriceSourcePreferenceProvider>();
        services.AddSingleton<IMarketService, MarketService>();
        services.AddSingleton<BeltFlipCalculator>();
        services.AddSingleton<IManufacturingFacilityConfigurationService, ManufacturingFacilityConfigurationService>();
        services.AddSingleton<IOwnedBlueprintWorkflowService, OwnedBlueprintWorkflowService>();
        services.AddSingleton<ShoppingListAggregator>();
        services.AddSingleton<IShoppingListService, ShoppingListService>();
        services.AddSingleton<ManufacturingAnalysisService>();

        services.AddSingleton<IPhase11SampleDataProvider, Phase11SampleDataProvider>();
        services.AddSingleton<IApplicationRestartService, ApplicationRestartService>();
        services.AddSingleton<IModalDialogService, ModalDialogService>();
        services.AddSingleton<ICharacterManagementQueryService, CharacterManagementQueryService>();
        services.AddSingleton<ICharacterManagementCommandService, CharacterManagementService>();
        services.AddSingleton<IBlueprintManagementQueryService, BlueprintManagementQueryService>();
        services.AddSingleton<IBlueprintManagementCommandService, BlueprintManagementCommandService>();
        services.AddSingleton<IManufacturingWorkspaceQueryService, ManufacturingWorkspaceQueryService>();
        services.AddSingleton<IManufacturingWorkspaceCommandService, ManufacturingWorkspaceCommandService>();
        services.AddSingleton<IMarketPriceQueryService, MarketPriceQueryService>();
        services.AddSingleton<IMarketPriceCommandService, MarketPriceCommandService>();
        services.AddSingleton<IStructureFacilityManagementQueryService, StructureFacilityManagementQueryService>();
        services.AddSingleton<IStructureFacilityManagementCommandService, StructureFacilityManagementCommandService>();
        services.AddSingleton<IShellDialogService, ShellDialogService>();
        services.AddSingleton<ILegacyDatabaseImportService, LegacyDatabaseImportService>();
        services.AddSingleton<IAssetsQueryService, AssetsQueryService>();
        services.AddSingleton<IAssetsCommandService, AssetsCommandService>();
        services.AddSingleton<IIndustryJobsQueryService, IndustryJobsQueryService>();
        services.AddSingleton<IIndustryJobsCommandService, IndustryJobsCommandService>();
        services.AddSingleton<IMiningReprocessingWorkspaceQueryService, MiningReprocessingWorkspaceQueryService>();
        services.AddSingleton<IMiningReprocessingWorkspaceCommandService, MiningReprocessingWorkspaceCommandService>();
        services.AddSingleton<IShoppingListWorkspaceQueryService, ShoppingListWorkspaceQueryService>();
        services.AddSingleton<IShoppingListWorkspaceCommandService, ShoppingListWorkspaceCommandService>();
        services.AddSingleton<IResearchAgentsScreenService>(provider =>
            new ResearchAgentsScreenService(
                provider.GetRequiredService<IPhase11SampleDataProvider>(),
                provider.GetRequiredService<IResearchAgentDatacoreService>(),
                provider.GetService<ICharacterRepository>(),
                provider.GetService<IResearchAgentService>()));

        services.AddSingleton<CharacterManagementViewModel>();
        services.AddSingleton<BlueprintManagementViewModel>();
        services.AddSingleton<ManufacturingWorkspaceViewModel>();
        services.AddSingleton<MarketPriceViewModel>();
        services.AddSingleton<MiningReprocessingViewModel>();
        services.AddSingleton<StructureFacilityManagementViewModel>();
        services.AddSingleton<AssetsViewModel>();
        services.AddSingleton<IndustryJobsViewModel>();
        services.AddSingleton<ShoppingListViewModel>();
        services.AddSingleton<ResearchAgentsViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}