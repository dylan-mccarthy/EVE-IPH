using EVE.IPH.Domain.Reprocessing.Services;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.Tests.Services;

public sealed class MiningReprocessingWorkspaceServiceTests
{
    [Fact]
    public async Task QueryService_GetScreenDataAsync_ReturnsBeltFlipDefaults()
    {
        MiningReprocessingWorkspaceQueryService service = new();

        MiningReprocessingScreenData result = await service.GetScreenDataAsync();

        result.BeltLinesText.Should().Contain("Veldspar");
        result.MiningVolumePerHourPerMiner.Should().Be(3600d);
        result.MinerCount.Should().Be(2);
    }

    [Fact]
    public async Task CommandService_CalculateBeltFlipAsync_MapsSuccessfulCalculation()
    {
        MiningReprocessingWorkspaceCommandService service = new(new BeltFlipCalculator());

        var result = await service.CalculateBeltFlipAsync(new MiningReprocessingRequest(
            "Veldspar|1000|0.1|15|22000|350\nScordite|500|0.15|18|11500|120",
            3600d,
            2,
            false,
            false));

        result.IsSuccess.Should().BeTrue();
        result.Value.Result.BetterOutcome.Should().Be("Reprocess");
        result.Value.Result.RefinedSaleValue.Should().Be(33500d);
        result.Value.StatusText.Should().Contain("2 belt lines");
    }

    [Fact]
    public async Task CommandService_CalculateBeltFlipAsync_WhenInputFormatInvalid_ReturnsFailure()
    {
        MiningReprocessingWorkspaceCommandService service = new(new BeltFlipCalculator());

        var result = await service.CalculateBeltFlipAsync(new MiningReprocessingRequest(
            "Veldspar|oops",
            3600d,
            2,
            false,
            false));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_BELT_LINE_FORMAT");
    }
}