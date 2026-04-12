using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.UI.Avalonia.Services;
using EVE.IPH.UI.Avalonia.ViewModels;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.ViewModels;

public sealed class ManufacturingWorkspaceViewModelTests
{
    [Fact]
    public async Task LoadTask_LoadsOptionsAndDefaults()
    {
        IManufacturingWorkspaceQueryService queryService = Substitute.For<IManufacturingWorkspaceQueryService>();
        IManufacturingWorkspaceCommandService commandService = Substitute.For<IManufacturingWorkspaceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(BuildScreenData());

        ManufacturingWorkspaceViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        viewModel.Blueprints.Should().ContainSingle();
        viewModel.Facilities.Should().ContainSingle();
        viewModel.SelectedBlueprint.Should().NotBeNull();
        viewModel.SelectedFacility.Should().NotBeNull();
        viewModel.UserRuns.Should().Be(2);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenSuccessful_UpdatesProjectedResult()
    {
        IManufacturingWorkspaceQueryService queryService = Substitute.For<IManufacturingWorkspaceQueryService>();
        IManufacturingWorkspaceCommandService commandService = Substitute.For<IManufacturingWorkspaceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(BuildScreenData());
        commandService.AnalyzeAsync(Arg.Any<ManufacturingWorkspaceAnalysisRequest>(), Arg.Any<CancellationToken>()).Returns(
            Result<ManufacturingWorkspaceAnalysisResult>.Success(new ManufacturingWorkspaceAnalysisResult(
                "28607",
                "Vargur",
                "Tatara Alpha",
                "Kara Maken",
                true,
                true,
                true,
                2,
                450_000_000,
                430_000_000,
                50_000_000,
                70_000_000,
                3_500_000,
                4_200_000,
                1_200_000,
                42000,
                36000,
                "Calculated.")));

        ManufacturingWorkspaceViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        await viewModel.AnalyzeAsync();

        viewModel.AnalysisResult.Should().NotBeNull();
        viewModel.StatusText.Should().Be("Calculated.");
        viewModel.HasAnalysisResult.Should().BeTrue();
    }

    [Fact]
    public async Task SelectedFacility_WhenChanged_ClearsPreviousAnalysis()
    {
        IManufacturingWorkspaceQueryService queryService = Substitute.For<IManufacturingWorkspaceQueryService>();
        IManufacturingWorkspaceCommandService commandService = Substitute.For<IManufacturingWorkspaceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(new ManufacturingWorkspaceScreenData(
            [new ManufacturingBlueprintOption(1001, "Kara Maken", false, new BlueprintId(28607), "Vargur Blueprint", 10, 20, 2, 1, true)],
            [
                new ManufacturingFacilityOption(new CharacterId(1001), FacilityProductionType.Manufacturing, 4001, "Tatara Alpha", "Kara Maken", "The Forge", "Jita", 0.035, 0.01),
                new ManufacturingFacilityOption(new CharacterId(1002), FacilityProductionType.Manufacturing, 4002, "Azbel Beta", "Nesra Velen", "Domain", "Amarr", 0.025, 0.008),
            ],
            "Loaded manufacturing workspace."));
        commandService.AnalyzeAsync(Arg.Any<ManufacturingWorkspaceAnalysisRequest>(), Arg.Any<CancellationToken>()).Returns(
            Result<ManufacturingWorkspaceAnalysisResult>.Success(new ManufacturingWorkspaceAnalysisResult(
                "28607",
                "Vargur",
                "Tatara Alpha",
                "Kara Maken",
                true,
                true,
                true,
                2,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                "Calculated.")));

        ManufacturingWorkspaceViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;
        await viewModel.AnalyzeAsync();

        viewModel.SelectedFacility = viewModel.Facilities.Last();

        viewModel.AnalysisResult.Should().BeNull();
        viewModel.StatusText.Should().Contain("selection changed");
    }

    [Fact]
    public async Task AnalyzeAsync_WhenCommandFails_ExposesFailureStatus()
    {
        IManufacturingWorkspaceQueryService queryService = Substitute.For<IManufacturingWorkspaceQueryService>();
        IManufacturingWorkspaceCommandService commandService = Substitute.For<IManufacturingWorkspaceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(BuildScreenData());
        commandService.AnalyzeAsync(Arg.Any<ManufacturingWorkspaceAnalysisRequest>(), Arg.Any<CancellationToken>()).Returns(
            Result<ManufacturingWorkspaceAnalysisResult>.Failure("INVALID_TOTAL_PRODUCTION_TIME", "Total production time must be greater than zero."));

        ManufacturingWorkspaceViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        await viewModel.AnalyzeAsync();

        viewModel.AnalysisResult.Should().BeNull();
        viewModel.StatusText.Should().Contain("Unable to analyze manufacturing");
    }

    private static ManufacturingWorkspaceScreenData BuildScreenData() => new(
        [new ManufacturingBlueprintOption(1001, "Kara Maken", false, new BlueprintId(28607), "Vargur Blueprint", 10, 20, 2, 1, true)],
        [new ManufacturingFacilityOption(new CharacterId(1001), FacilityProductionType.Manufacturing, 4001, "Tatara Alpha", "Kara Maken", "The Forge", "Jita", 0.035, 0.01)],
        "Loaded manufacturing workspace.");
}