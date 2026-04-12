using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Assets.Services;
using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Industry.Models;
using EVE.IPH.UI.Avalonia.Services;
using EVE.IPH.UI.Avalonia.ViewModels;
using EVE.IPH.Infrastructure.Settings.Models;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task Constructor_WhenLegacyDatabaseDetected_EnablesImportPrompt()
    {
        ILegacyDatabaseImportService importService = Substitute.For<ILegacyDatabaseImportService>();
        importService.GetDetectedLegacyDatabasePath().Returns("C:\\legacy\\EVEIPH DB.sqlite");

        MainWindowViewModel viewModel = await CreateViewModelAsync(importService, Substitute.For<IApplicationRestartService>());

        viewModel.CanImportLegacyDatabase.Should().BeTrue();
        viewModel.LegacyImportSourcePath.Should().Be("C:\\legacy\\EVEIPH DB.sqlite");
        viewModel.SupportedStaticDataBuild.Should().Be(3294658);
        viewModel.ImportedStaticDataBuildText.Should().Be("3294658");
        viewModel.CheckForUpdatesOnStart.Should().BeTrue();
        viewModel.LegacyImportStatus.Should().Contain("Legacy database detected");
    }

    [Fact]
    public async Task Constructor_WhenNoLegacyDatabaseDetected_ShowsBrowseGuidance()
    {
        ILegacyDatabaseImportService importService = Substitute.For<ILegacyDatabaseImportService>();
        importService.GetDetectedLegacyDatabasePath().Returns((string?)null);

        MainWindowViewModel viewModel = await CreateViewModelAsync(importService, Substitute.For<IApplicationRestartService>());

        viewModel.CanImportLegacyDatabase.Should().BeFalse();
        viewModel.LegacyImportStatus.Should().Contain("browse to an older SQLite database");
    }

    [Fact]
    public async Task ImportLegacyDatabaseFromPathAsync_WhenSuccessful_UpdatesRestartState()
    {
        ILegacyDatabaseImportService importService = Substitute.For<ILegacyDatabaseImportService>();
        importService.GetDetectedLegacyDatabasePath().Returns("C:\\legacy\\EVEIPH DB.sqlite");
        LegacyDatabaseImportScreenResult importResult = new("C:\\legacy\\EVEIPH DB.sqlite", "C:\\appdata\\EVEIPH DB.sqlite", "C:\\appdata\\EVEIPH DB.sqlite.backup");
        importService.ImportAsync("C:\\legacy\\EVEIPH DB.sqlite", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(importResult));

        MainWindowViewModel viewModel = await CreateViewModelAsync(importService, Substitute.For<IApplicationRestartService>());

        LegacyDatabaseImportScreenResult? result = await viewModel.ImportLegacyDatabaseFromPathAsync("C:\\legacy\\EVEIPH DB.sqlite");

        result.Should().Be(importResult);
        viewModel.RestartRequired.Should().BeTrue();
        viewModel.CanBrowseLegacyDatabase.Should().BeFalse();
        viewModel.LegacyImportSourcePath.Should().BeNull();
        viewModel.LegacyImportStatus.Should().Contain("Restart the app now");
        viewModel.LegacyImportStatus.Should().Contain("backup");
    }

    [Fact]
    public async Task ImportLegacyDatabaseFromPathAsync_WhenImportFails_ExposesFailureStatus()
    {
        ILegacyDatabaseImportService importService = Substitute.For<ILegacyDatabaseImportService>();
        importService.GetDetectedLegacyDatabasePath().Returns((string?)null);
        importService.ImportAsync("C:\\legacy\\broken.sqlite", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<LegacyDatabaseImportScreenResult>(new InvalidOperationException("broken import")));

        MainWindowViewModel viewModel = await CreateViewModelAsync(importService, Substitute.For<IApplicationRestartService>());

        LegacyDatabaseImportScreenResult? result = await viewModel.ImportLegacyDatabaseFromPathAsync("C:\\legacy\\broken.sqlite");

        result.Should().BeNull();
        viewModel.RestartRequired.Should().BeFalse();
        viewModel.LegacyImportStatus.Should().Contain("broken import");
    }

    [Fact]
    public async Task ShouldConfirmLegacyDatabaseImport_UsesImportServiceDecision()
    {
        ILegacyDatabaseImportService importService = Substitute.For<ILegacyDatabaseImportService>();
        importService.GetDetectedLegacyDatabasePath().Returns((string?)null);
        importService.ImportWouldOverwrite("C:\\legacy\\EVEIPH DB.sqlite").Returns(true);

        MainWindowViewModel viewModel = await CreateViewModelAsync(importService, Substitute.For<IApplicationRestartService>());

        viewModel.ShouldConfirmLegacyDatabaseImport("C:\\legacy\\EVEIPH DB.sqlite").Should().BeTrue();
    }

    [Fact]
    public async Task TryRestartApplication_WhenRestartFails_ReturnsErrorMessage()
    {
        IApplicationRestartService restartService = Substitute.For<IApplicationRestartService>();
        restartService.When(service => service.Restart()).Do(_ => throw new InvalidOperationException("restart failed"));

        MainWindowViewModel viewModel = await CreateViewModelAsync(Substitute.For<ILegacyDatabaseImportService>(), restartService);

        string? error = viewModel.TryRestartApplication();

        error.Should().Be("restart failed");
    }

    [Fact]
    public async Task SaveStartupPreferencesAsync_WhenSuccessful_UpdatesPersistedShellState()
    {
        ILegacyDatabaseImportService importService = Substitute.For<ILegacyDatabaseImportService>();
        ISettingsShellCommandService commandService = Substitute.For<ISettingsShellCommandService>();
        commandService.SaveStartupPreferencesAsync(Arg.Any<SettingsShellStartupPreferencesRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<SettingsShellScreenData>.Success(CreateSettingsShellScreenData(new ApplicationSettingsModel
            {
                CheckForUpdatesOnStart = false,
                LoadAssetsOnStartup = false,
                LoadBpsOnStartup = true,
                LoadEsiMarketDataOnStartup = false,
                LoadEsiSystemCostIndicesOnStartup = true,
                LoadEsiPublicStructuresOnStartup = false,
            })));

        MainWindowViewModel viewModel = await CreateViewModelAsync(importService, Substitute.For<IApplicationRestartService>(), commandService: commandService);
        viewModel.CheckForUpdatesOnStart = false;
        viewModel.LoadAssetsOnStartup = false;
        viewModel.LoadBlueprintsOnStartup = true;
        viewModel.LoadMarketDataOnStartup = false;
        viewModel.LoadSystemCostIndicesOnStartup = true;
        viewModel.LoadPublicStructuresOnStartup = false;

        await viewModel.SaveStartupPreferencesAsync();

        viewModel.StartupPreferencesStatusText.Should().Contain("startup preference");
        await commandService.Received(1).SaveStartupPreferencesAsync(
            Arg.Is<SettingsShellStartupPreferencesRequest>(request =>
                !request.CheckForUpdatesOnStart &&
                !request.LoadAssetsOnStartup &&
                request.LoadBlueprintsOnStartup &&
                !request.LoadMarketDataOnStartup &&
                request.LoadSystemCostIndicesOnStartup &&
                !request.LoadPublicStructuresOnStartup),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenUpdateIsReady_UpdatesShellStatus()
    {
        ILegacyDatabaseImportService importService = Substitute.For<ILegacyDatabaseImportService>();
        IUpdateShellService updateShellService = Substitute.For<IUpdateShellService>();
        updateShellService.GetCurrentStatus().Returns(new UpdateShellStatus(
            "Shell-driven update checks are available for this packaged install. Current version: 1.0.0.",
            CanCheckForUpdates: true,
            CanApplyPreparedUpdate: false));
        updateShellService.CheckForUpdatesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new UpdateShellStatus(
            "Update 1.1.0 has been downloaded and is ready to apply. Restart from the shell to finish installing it.",
            CanCheckForUpdates: true,
            CanApplyPreparedUpdate: true)));

        MainWindowViewModel viewModel = await CreateViewModelAsync(
            importService,
            Substitute.For<IApplicationRestartService>(),
            updateShellService: updateShellService);

        await viewModel.CheckForUpdatesAsync();

        viewModel.UpdateStatusText.Should().Contain("1.1.0");
        viewModel.CanApplyPreparedUpdate.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyPreparedUpdateAndRestart_WhenServiceReturnsFailureStatus_UpdatesShellMessage()
    {
        ILegacyDatabaseImportService importService = Substitute.For<ILegacyDatabaseImportService>();
        IUpdateShellService updateShellService = Substitute.For<IUpdateShellService>();
        updateShellService.GetCurrentStatus().Returns(new UpdateShellStatus(
            "Update 1.1.0 has been downloaded and is ready to apply. Restart from the shell to finish installing it.",
            CanCheckForUpdates: true,
            CanApplyPreparedUpdate: true));
        updateShellService.ApplyPreparedUpdateAndRestart().Returns(new UpdateShellStatus(
            "Unable to apply the downloaded update automatically: restart helper missing",
            CanCheckForUpdates: true,
            CanApplyPreparedUpdate: true));

        MainWindowViewModel viewModel = await CreateViewModelAsync(
            importService,
            Substitute.For<IApplicationRestartService>(),
            updateShellService: updateShellService);

        viewModel.ApplyPreparedUpdateAndRestart();

        viewModel.UpdateStatusText.Should().Contain("restart helper missing");
        viewModel.CanApplyPreparedUpdate.Should().BeTrue();
    }

    private static async Task<MainWindowViewModel> CreateViewModelAsync(
        ILegacyDatabaseImportService importService,
        IApplicationRestartService restartService,
        ISettingsShellQueryService? queryService = null,
        ISettingsShellCommandService? commandService = null,
        IUpdateShellService? updateShellService = null)
    {
        importService.GetDetectedLegacyDatabasePath().Returns(importService.GetDetectedLegacyDatabasePath());
        queryService ??= Substitute.For<ISettingsShellQueryService>();
        commandService ??= Substitute.For<ISettingsShellCommandService>();
        updateShellService ??= Substitute.For<IUpdateShellService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(CreateSettingsShellScreenData(new ApplicationSettingsModel()));
        updateShellService.GetCurrentStatus().Returns(new UpdateShellStatus(
            "Shell-driven update checks are available for this packaged install. Current version: 1.0.0.",
            CanCheckForUpdates: true,
            CanApplyPreparedUpdate: false));

        CharacterManagementViewModel characterManagement = await CreateCharacterManagementViewModelAsync();
        BlueprintManagementViewModel blueprints = await CreateBlueprintManagementViewModelAsync();
        ManufacturingWorkspaceViewModel manufacturing = await CreateManufacturingWorkspaceViewModelAsync();
        MarketPriceViewModel market = await CreateMarketPriceViewModelAsync();
        ShoppingListViewModel shoppingList = await CreateShoppingListViewModelAsync();
        MiningReprocessingViewModel miningReprocessing = await CreateMiningReprocessingViewModelAsync();
        StructureFacilityManagementViewModel structureFacilities = await CreateStructureFacilityManagementViewModelAsync();
        AssetsViewModel assets = await CreateAssetsViewModelAsync();
        IndustryJobsViewModel industryJobs = await CreateIndustryJobsViewModelAsync();
        ResearchAgentsViewModel researchAgents = await CreateResearchAgentsViewModelAsync();

        return new MainWindowViewModel(
            characterManagement,
            blueprints,
            manufacturing,
            market,
            shoppingList,
            miningReprocessing,
            structureFacilities,
            assets,
            industryJobs,
            researchAgents,
            importService,
            restartService,
            queryService,
                commandService,
                updateShellService);
    }

    private static SettingsShellScreenData CreateSettingsShellScreenData(ApplicationSettingsModel settings) => new(
        "C:\\Users\\tester\\AppData\\Roaming\\EVE-IPH\\EVEIPH DB.sqlite",
        "Database import, startup loading preferences, and update-check behavior are now backed by persisted shell settings instead of placeholder status text.",
        "Startup data loading is configured from the persisted application settings below. The onboarding dialog remains available from the shell for first-run guidance.",
        settings.CheckForUpdatesOnStart
            ? "Automatic update checks on startup are enabled in the persisted application settings. Velopack bootstrap is live; richer update workflows still follow later milestones."
            : "Automatic update checks on startup are disabled in the persisted application settings. Velopack bootstrap remains wired, but shell-driven check/apply flows are still deferred.",
        3294658,
        "3294658",
        "https://developers.eveonline.com/static-data/tranquility/eve-online-static-data-3294658-jsonl.zip",
        "2026-04-12 00:00:00Z",
        settings.CheckForUpdatesOnStart,
        settings.LoadAssetsOnStartup,
        settings.LoadBpsOnStartup,
        settings.LoadEsiMarketDataOnStartup,
        settings.LoadEsiSystemCostIndicesOnStartup,
        settings.LoadEsiPublicStructuresOnStartup,
        "4 startup preferences currently enabled. Save changes here to persist how much shell data should preload on startup.",
        settings);

    private static async Task<CharacterManagementViewModel> CreateCharacterManagementViewModelAsync()
    {
        ICharacterManagementQueryService queryService = Substitute.For<ICharacterManagementQueryService>();
        ICharacterManagementCommandService commandService = Substitute.For<ICharacterManagementCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(
            Result<CharacterManagementScreenData>.Success(new CharacterManagementScreenData([], [], "No characters have been connected yet. Connect one to start syncing ESI data."))));

        CharacterManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;
        return viewModel;
    }

    private static async Task<AssetsViewModel> CreateAssetsViewModelAsync()
    {
        IAssetsQueryService queryService = Substitute.For<IAssetsQueryService>();
        IAssetsCommandService commandService = Substitute.For<IAssetsCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(
            new AssetsScreenData(
                [new HydratedAsset(1001, 5000001, 6000001, new TypeId(101), 12, 11, true, AssetBlueprintKind.None, string.Empty, "Heavy Water", "Ice Products", "Material", "Jita 4-4", "Item Hangar", false, 1)],
                [new AssetOwnerFilterOption(null, "All Owners"), new AssetOwnerFilterOption(1001, "Kara Maken")],
                "Loaded synced asset records from the local SQLite store.")));
        commandService.RefreshAsync(Arg.Any<CancellationToken>()).Returns(call => queryService.GetScreenDataAsync(call.Arg<CancellationToken>()));

        AssetsViewModel viewModel = new(new AssetViewFilterService(), queryService, commandService);
        await viewModel.LoadTask;
        return viewModel;
    }

    private static async Task<BlueprintManagementViewModel> CreateBlueprintManagementViewModelAsync()
    {
        IBlueprintManagementQueryService queryService = Substitute.For<IBlueprintManagementQueryService>();
        IBlueprintManagementCommandService commandService = Substitute.For<IBlueprintManagementCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(new BlueprintManagementScreenData(
            [new BlueprintManagementRow(1001, "Kara Maken", false, new ItemId(7000001), 60015068, new BlueprintId(28607), "Vargur Blueprint", 1, 10, 20, -1, 1, true, true)],
            [new BlueprintOwnerFilterOption(null, "All Owners"), new BlueprintOwnerFilterOption(1001, "Kara Maken")],
            "Loaded owned blueprints for the connected characters and corporations."));

        BlueprintManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;
        return viewModel;
    }

    private static async Task<ManufacturingWorkspaceViewModel> CreateManufacturingWorkspaceViewModelAsync()
    {
        IManufacturingWorkspaceQueryService queryService = Substitute.For<IManufacturingWorkspaceQueryService>();
        IManufacturingWorkspaceCommandService commandService = Substitute.For<IManufacturingWorkspaceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(new ManufacturingWorkspaceScreenData(
            [new ManufacturingBlueprintOption(1001, "Kara Maken", false, new BlueprintId(28607), "Vargur Blueprint", 10, 20, 2, 1, true)],
            [new ManufacturingFacilityOption(new CharacterId(1001), FacilityProductionType.Manufacturing, 4001, "Tatara Alpha", "Kara Maken", "The Forge", "Jita", 0.035, 0.01)],
            "Loaded manufacturing workspace."));

        ManufacturingWorkspaceViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;
        return viewModel;
    }

    private static async Task<MarketPriceViewModel> CreateMarketPriceViewModelAsync()
    {
        IMarketPriceQueryService queryService = Substitute.For<IMarketPriceQueryService>();
        IMarketPriceCommandService commandService = Substitute.For<IMarketPriceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(new MarketPriceScreenData(
            10000002,
            "34, 35, 36, 37",
            MarketPriceSourceKind.Fuzzworks,
            [
                new MarketPriceSourceOption(MarketPriceSourceKind.Tranquility, "Tranquility"),
                new MarketPriceSourceOption(MarketPriceSourceKind.EveMarketer, "EVE Marketer"),
                new MarketPriceSourceOption(MarketPriceSourceKind.Fuzzworks, "Fuzzworks"),
            ],
            "Enter item type IDs and a region ID to load live market prices through the modern market service."));

        MarketPriceViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;
        return viewModel;
    }

    private static async Task<StructureFacilityManagementViewModel> CreateStructureFacilityManagementViewModelAsync()
    {
        IStructureFacilityManagementQueryService queryService = Substitute.For<IStructureFacilityManagementQueryService>();
        IStructureFacilityManagementCommandService commandService = Substitute.For<IStructureFacilityManagementCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(new StructureFacilityManagementScreenData(
            [new StructureFacilityCharacterOption(new CharacterId(1001), "Kara Maken", true)],
            [new IndustryStructureRow(4001, "Tatara Alpha", 35835, 30000142, 10000002, 2001, true, DateTimeOffset.UtcNow)],
            [new FacilitySettingsRow(new CharacterId(1001), "Kara Maken", FacilityProductionType.Manufacturing, 4001, "Tatara Alpha", IndustryFacilityKind.UpwellStructure, 4001, "Tatara Alpha", 10000002, "The Forge", 30000142, "Jita", 0.9, 0.035, 0, true, true, true, false, 0, 0.01, null, null, null, [6001, 6002])],
            [new FacilityProductionTypeOption(FacilityProductionType.Manufacturing, "Manufacturing")],
            [new FacilityKindOption(IndustryFacilityKind.UpwellStructure, "Upwell Structure")],
            "Loaded structures and facilities."));

        StructureFacilityManagementViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;
        return viewModel;
    }

    private static async Task<MiningReprocessingViewModel> CreateMiningReprocessingViewModelAsync()
    {
        IMiningReprocessingWorkspaceQueryService queryService = Substitute.For<IMiningReprocessingWorkspaceQueryService>();
        IMiningReprocessingWorkspaceCommandService commandService = Substitute.For<IMiningReprocessingWorkspaceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(new MiningReprocessingScreenData(
            "Veldspar|1000|0.1|15|22000|350\nScordite|500|0.15|18|11500|120",
            3600d,
            2,
            false,
            false,
            "Enter belt lines to compare raw sale value versus refined sale value for a narrow belt-flip slice."));

        MiningReprocessingViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;
        return viewModel;
    }

    private static async Task<ShoppingListViewModel> CreateShoppingListViewModelAsync()
    {
        IShoppingListWorkspaceQueryService queryService = Substitute.For<IShoppingListWorkspaceQueryService>();
        IShoppingListWorkspaceCommandService commandService = Substitute.For<IShoppingListWorkspaceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(new ShoppingListScreenData(
            [new ShoppingListRow(34, "Tritanium", 100, 5d)],
            1,
            100,
            500d,
            "Loaded 1 persisted shopping-list row from the local SQLite store."));

        ShoppingListViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;
        return viewModel;
    }

    private static async Task<IndustryJobsViewModel> CreateIndustryJobsViewModelAsync()
    {
        IIndustryJobsQueryService queryService = Substitute.For<IIndustryJobsQueryService>();
        IIndustryJobsCommandService commandService = Substitute.For<IIndustryJobsCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(new IndustryJobsScreenData(
            new IndustryJobSummary(1, 0, 0, 0, 1, 0),
            [new IndustryJobDisplayRow(900001, "Kara Maken", "Manufacturing", "Vargur Blueprint", "Vargur", "Ship", "Jita", "The Forge", 1, 2, 0, "Tatara Alpha", "Ship Hangar", "Personal", IndustryJobState.InProgress, "In Progress", "Runs 0/2")],
            "Loaded synced industry-job records from the local SQLite store."));

        IndustryJobsViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;
        return viewModel;
    }

    private static async Task<ResearchAgentsViewModel> CreateResearchAgentsViewModelAsync()
    {
        IResearchAgentsScreenService screenService = Substitute.For<IResearchAgentsScreenService>();
        screenService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(
            new ResearchAgentsScreenData(
                new ResearchAgentDatacoreSummary([], 0),
                "Loaded")));

        ResearchAgentsViewModel viewModel = new(screenService);
        await viewModel.LoadTask;
        return viewModel;
    }
}