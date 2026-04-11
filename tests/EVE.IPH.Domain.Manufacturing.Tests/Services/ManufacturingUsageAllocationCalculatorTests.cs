using EVE.IPH.Domain.Manufacturing.Models;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class ManufacturingUsageAllocationCalculatorTests
{
    private readonly ManufacturingUsageAllocationCalculator _sut = new();

    [Fact]
    public void Calculate_WhenStandardManufacturingUsage_ReturnsMainAndComponentUsage()
    {
        ManufacturingUsageAllocationInput input = new(
            new ManufacturingFacilityUsageResult(180, 180, 180, 0, 0),
            IncludeComponentManufacturingUsage: true,
            ComponentFacilityUsage: 60,
            IncludeCapitalComponentManufacturingUsage: true,
            CapitalComponentFacilityUsage: 30,
            IncludeReactionUsage: false,
            TotalReactionFacilityUsage: 45,
            HasReprocessingFacility: true,
            IncludeReprocessingUsage: true,
            ReprocessingUsage: 12);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.MainFacilityUsage.Should().Be(180d);
        result.Value.ComponentUsage.Should().Be(90d);
        result.Value.RemainingReactionUsage.Should().Be(0d);
        result.Value.ReprocessingUsage.Should().Be(12d);
        result.Value.TotalUsage.Should().Be(282d);
    }

    [Fact]
    public void Calculate_WhenReactionIsMainFlow_UsesReactionAsMainAndTracksRemainingReactionUsage()
    {
        ManufacturingUsageAllocationInput input = new(
            new ManufacturingFacilityUsageResult(25, 25, 0, 25, 25),
            IncludeComponentManufacturingUsage: true,
            ComponentFacilityUsage: 10,
            IncludeCapitalComponentManufacturingUsage: false,
            CapitalComponentFacilityUsage: 0,
            IncludeReactionUsage: true,
            TotalReactionFacilityUsage: 40,
            HasReprocessingFacility: false,
            IncludeReprocessingUsage: false,
            ReprocessingUsage: 5);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.MainFacilityUsage.Should().Be(25d);
        result.Value.ComponentUsage.Should().Be(10d);
        result.Value.RemainingReactionUsage.Should().Be(15d);
        result.Value.ReprocessingUsage.Should().Be(5d);
        result.Value.TotalUsage.Should().Be(55d);
    }

    [Fact]
    public void Calculate_WhenReprocessingUsageExcluded_ZeroesReprocessingUsage()
    {
        ManufacturingUsageAllocationInput input = new(
            new ManufacturingFacilityUsageResult(0, 0, 0, 0, 0),
            IncludeComponentManufacturingUsage: false,
            ComponentFacilityUsage: 0,
            IncludeCapitalComponentManufacturingUsage: false,
            CapitalComponentFacilityUsage: 0,
            IncludeReactionUsage: false,
            TotalReactionFacilityUsage: 0,
            HasReprocessingFacility: true,
            IncludeReprocessingUsage: false,
            ReprocessingUsage: 20);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReprocessingUsage.Should().Be(0d);
    }

    [Fact]
    public void Calculate_WhenUsageValuesAreNegative_ReturnsFailure()
    {
        ManufacturingUsageAllocationInput input = new(
            new ManufacturingFacilityUsageResult(0, 0, 0, 0, 0),
            IncludeComponentManufacturingUsage: true,
            ComponentFacilityUsage: -1,
            IncludeCapitalComponentManufacturingUsage: false,
            CapitalComponentFacilityUsage: 0,
            IncludeReactionUsage: false,
            TotalReactionFacilityUsage: 0,
            HasReprocessingFacility: false,
            IncludeReprocessingUsage: false,
            ReprocessingUsage: 0);

        var result = _sut.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_USAGE_VALUE");
    }
}