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
using NSubstitute;

using EVE.IPH.Infrastructure.Settings.Models;
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

    private static async Task<MainWindowViewModel> CreateViewModelAsync(
        ILegacyDatabaseImportService importService,
        IApplicationRestartService restartService)
    {
        importService.GetDetectedLegacyDatabasePath().Returns(importService.GetDetectedLegacyDatabasePath());

        CharacterManagementViewModel characterManagement = await CreateCharacterManagementViewModelAsync();
        BlueprintManagementViewModel blueprints = await CreateBlueprintManagementViewModelAsync();
        ManufacturingWorkspaceViewModel manufacturing = await CreateManufacturingWorkspaceViewModelAsync();
        StructureFacilityManagementViewModel structureFacilities = await CreateStructureFacilityManagementViewModelAsync();
        AssetsViewModel assets = await CreateAssetsViewModelAsync();
        IndustryJobsViewModel industryJobs = await CreateIndustryJobsViewModelAsync();
        ResearchAgentsViewModel researchAgents = await CreateResearchAgentsViewModelAsync();

        return new MainWindowViewModel(
            characterManagement,
            blueprints,
            manufacturing,
            structureFacilities,
            assets,
            industryJobs,
            researchAgents,
            importService,
            restartService,
            new StaticDataSettingsModel
            {
                SupportedBuildNumber = 3294658,
                ImportedBuildNumber = 3294658,
                ImportedAtUtc = new DateTimeOffset(2026, 4, 12, 0, 0, 0, TimeSpan.Zero),
            });
    }

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