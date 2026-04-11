using EVE.IPH.Domain.Manufacturing.Models;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class ManufacturingTimelineCalculatorTests
{
    [Fact]
    public void Calculate_WithComponentAndActivityTime_ReturnsLegacyRollup()
    {
        ManufacturingTimelineInput input = new(
            BaseBlueprintProductionTimeSeconds: 28_800,
            CopyTimeSeconds: 10_120.95,
            InventionTimeSeconds: 9_792,
            ComponentProductionTimeSeconds: 14_400);

        ManufacturingTimelineCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.BlueprintProductionTimeSeconds.Should().BeApproximately(48_712.95d, 0.000001d);
        result.Value.TotalProductionTimeSeconds.Should().BeApproximately(63_112.95d, 0.000001d);
    }

    [Fact]
    public void Calculate_WithoutComponentTime_UsesBlueprintTimelineOnly()
    {
        ManufacturingTimelineInput input = new(
            BaseBlueprintProductionTimeSeconds: 12_000,
            CopyTimeSeconds: 0,
            InventionTimeSeconds: 3_600,
            ComponentProductionTimeSeconds: 0);

        ManufacturingTimelineCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.BlueprintProductionTimeSeconds.Should().Be(15_600d);
        result.Value.TotalProductionTimeSeconds.Should().Be(15_600d);
    }

    [Fact]
    public void Calculate_WhenAllTimesAreZero_ReturnsZeroTimeline()
    {
        ManufacturingTimelineInput input = new(
            BaseBlueprintProductionTimeSeconds: 0,
            CopyTimeSeconds: 0,
            InventionTimeSeconds: 0,
            ComponentProductionTimeSeconds: 0);

        ManufacturingTimelineCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.BlueprintProductionTimeSeconds.Should().Be(0);
        result.Value.TotalProductionTimeSeconds.Should().Be(0);
    }

    [Fact]
    public void Calculate_WhenAnyTimeIsNegative_ReturnsFailure()
    {
        ManufacturingTimelineInput input = new(
            BaseBlueprintProductionTimeSeconds: -1,
            CopyTimeSeconds: 0,
            InventionTimeSeconds: 0,
            ComponentProductionTimeSeconds: 0);

        ManufacturingTimelineCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_PRODUCTION_TIME");
    }
}