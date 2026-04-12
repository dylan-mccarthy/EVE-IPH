using EVE.IPH.Domain.Core.Results;
using EVE.IPH.UI.Avalonia.Services;
using EVE.IPH.UI.Avalonia.ViewModels;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.ViewModels;

public sealed class MiningReprocessingViewModelTests
{
    [Fact]
    public async Task LoadTask_LoadsDefaults()
    {
        IMiningReprocessingWorkspaceQueryService queryService = Substitute.For<IMiningReprocessingWorkspaceQueryService>();
        IMiningReprocessingWorkspaceCommandService commandService = Substitute.For<IMiningReprocessingWorkspaceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(CreateScreenData());

        MiningReprocessingViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        viewModel.BeltLinesText.Should().Contain("Veldspar");
        viewModel.MiningVolumePerHourPerMiner.Should().Be(3600d);
        viewModel.MinerCount.Should().Be(2);
    }

    [Fact]
    public async Task CalculateAsync_WhenSuccessful_UpdatesResult()
    {
        IMiningReprocessingWorkspaceQueryService queryService = Substitute.For<IMiningReprocessingWorkspaceQueryService>();
        IMiningReprocessingWorkspaceCommandService commandService = Substitute.For<IMiningReprocessingWorkspaceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(CreateScreenData());
        commandService.CalculateBeltFlipAsync(Arg.Any<MiningReprocessingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<MiningReprocessingResult>.Success(new MiningReprocessingResult(
                new BeltFlipResultRow("Reprocess", 24000d, 33500d, 175d, 175d, 0.0243d, 0.0486d, 987654d, 1377777d, 470d),
                "Calculated a belt-flip comparison for 2 belt lines. Better outcome: Reprocess.")));

        MiningReprocessingViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        await viewModel.CalculateAsync();

        viewModel.Result.Should().NotBeNull();
        viewModel.Result!.BetterOutcome.Should().Be("Reprocess");
        viewModel.StatusText.Should().Contain("Calculated a belt-flip comparison");
    }

    [Fact]
    public async Task CalculateAsync_WhenCommandFails_ExposesFailureStatus()
    {
        IMiningReprocessingWorkspaceQueryService queryService = Substitute.For<IMiningReprocessingWorkspaceQueryService>();
        IMiningReprocessingWorkspaceCommandService commandService = Substitute.For<IMiningReprocessingWorkspaceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(CreateScreenData());
        commandService.CalculateBeltFlipAsync(Arg.Any<MiningReprocessingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<MiningReprocessingResult>.Failure("INVALID_BELT_LINE_FORMAT", "bad line"));

        MiningReprocessingViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        await viewModel.CalculateAsync();

        viewModel.Result.Should().BeNull();
        viewModel.StatusText.Should().Contain("Unable to calculate the belt flip").And.Contain("bad line");
    }

    private static MiningReprocessingScreenData CreateScreenData() => new(
        "Veldspar|1000|0.1|15|22000|350\nScordite|500|0.15|18|11500|120",
        3600d,
        2,
        false,
        false,
        "Enter belt lines to compare raw sale value versus refined sale value for a narrow belt-flip slice.");
}