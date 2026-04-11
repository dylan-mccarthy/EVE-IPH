using EVE.IPH.Domain.Manufacturing.Models;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class ManufacturingFacilityUsageCalculatorTests
{
    private readonly ManufacturingFacilityUsageCalculator _sut = new();

    [Fact]
    public void Calculate_WhenManufacturingUsageExcluded_ReturnsZeroUsage()
    {
        ManufacturingFacilityUsageInput input = new(false, 100, 10, 0.01, 1, 1, 0.02, 0.03, false, 0, false, false);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.FacilityUsage.Should().Be(0);
        result.Value.MainFacilityUsage.Should().Be(0);
    }

    [Fact]
    public void Calculate_WhenStandardManufacturing_ReturnsManufacturingUsage()
    {
        ManufacturingFacilityUsageInput input = new(true, 250, 36, 0.01, 1, 1, 0.005, 0.005, false, 0, false, false);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.FacilityUsage.Should().BeApproximately(180d, 0.000001d);
        result.Value.ManufacturingFacilityUsage.Should().BeApproximately(180d, 0.000001d);
        result.Value.ReactionFacilityUsage.Should().Be(0);
    }

    [Fact]
    public void Calculate_WhenReactionManufacturing_ReturnsReactionUsage()
    {
        ManufacturingFacilityUsageInput input = new(true, 100, 10, 0.005, 1, 1, 0.005, 0, false, 0, true, false);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.MainFacilityUsage.Should().BeApproximately(10d, 0.000001d);
        result.Value.ReactionFacilityUsage.Should().BeApproximately(10d, 0.000001d);
        result.Value.TotalReactionFacilityUsageDelta.Should().BeApproximately(10d, 0.000001d);
    }

    [Fact]
    public void Calculate_WhenAlphaAndFulcrumReductionApply_UsesModifiedSccAndAlphaTax()
    {
        ManufacturingFacilityUsageInput input = new(true, 100, 10, 0.01, 1, 1, 0.005, 0.02, true, 0.03, false, true);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.FacilityUsage.Should().BeApproximately(47d, 0.000001d);
    }
}