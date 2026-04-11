using EVE.IPH.Domain.Manufacturing.Models;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class ComponentProductionScheduleCalculatorTests
{
    [Fact]
    public void Calculate_WhenNoComponentTimes_ReturnsZero()
    {
        ComponentProductionScheduleInput input = new([], 3);

        ComponentProductionScheduleCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalComponentProductionTimeSeconds.Should().Be(0);
    }

    [Fact]
    public void Calculate_WhenOnlyOneProductionLine_ReturnsSumOfTimes()
    {
        ComponentProductionScheduleInput input = new([120d, 240d, 360d], 1);

        ComponentProductionScheduleCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalComponentProductionTimeSeconds.Should().Be(720d);
    }

    [Fact]
    public void Calculate_WhenComponentCountFitsAvailableLines_ReturnsMaxTime()
    {
        ComponentProductionScheduleInput input = new([600d, 300d, 120d], 3);

        ComponentProductionScheduleCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalComponentProductionTimeSeconds.Should().Be(600d);
    }

    [Fact]
    public void Calculate_WhenLongestComponentDominates_ReturnsLongestTime()
    {
        ComponentProductionScheduleInput input = new([600d, 60d, 60d, 60d], 2);

        ComponentProductionScheduleCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalComponentProductionTimeSeconds.Should().Be(600d);
    }

    [Fact]
    public void Calculate_WhenExtraSessionsAreNeeded_ReturnsRecursiveLegacyTime()
    {
        ComponentProductionScheduleInput input = new([600d, 540d, 480d, 420d, 360d], 2);

        ComponentProductionScheduleCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalComponentProductionTimeSeconds.Should().Be(1980d);
    }

    [Fact]
    public void Calculate_WhenLinesAreInvalid_ReturnsFailure()
    {
        ComponentProductionScheduleInput input = new([60d], 0);

        ComponentProductionScheduleCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_PRODUCTION_LINES");
    }
}