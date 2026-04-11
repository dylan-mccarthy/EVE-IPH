using EVE.IPH.Domain.Manufacturing.Models;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class ManufacturingBuildBuyDeciderTests
{
    private readonly ManufacturingBuildBuyDecider _sut = new();

    [Fact]
    public void Calculate_WhenFuelBlocksAreAlwaysBought_ReturnsBuy()
    {
        ManufacturingBuildBuyInput input = new("Helium Fuel Block", 10, 1, 1, 1, 0, true, true, false, true, false, false, null);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.BuildItem.Should().BeFalse();
    }

    [Fact]
    public void Calculate_WhenNewRequestAndBuildIsCheaper_ReturnsBuildBasedOnOwnershipSetting()
    {
        ManufacturingBuildBuyInput input = new("Widget", 100, 2, 5, 900, 50, false, true, true, false, false, false, null);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.CheaperToBuild.Should().BeTrue();
        result.Value.BuildItem.Should().BeTrue();
    }

    [Fact]
    public void Calculate_WhenManualOverrideExistsOnExistingRequest_UsesOverride()
    {
        ManufacturingBuildBuyInput input = new("Widget", 100, 1, 1, 10, 0, true, false, false, false, false, false, false);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.BuildItem.Should().BeFalse();
    }

    [Fact]
    public void Calculate_WhenMarketInsufficient_ForceBuilds()
    {
        ManufacturingBuildBuyInput input = new("Widget", 100, 1, 1, 500, 0, false, false, false, false, false, true, null);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.BuildItem.Should().BeTrue();
    }

    [Fact]
    public void Calculate_WhenQuantitiesInvalid_ReturnsFailure()
    {
        ManufacturingBuildBuyInput input = new("Widget", 100, 0, 1, 10, 0, false, true, false, false, false, false, null);

        var result = _sut.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_BUILD_BUY_QUANTITY");
    }
}