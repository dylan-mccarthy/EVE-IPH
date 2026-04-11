using EVE.IPH.Domain.Manufacturing.Models;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class ManufacturingCostCalculatorTests
{
    [Fact]
    public void Calculate_WithIncludedCopyAndInventionCosts_ReturnsLegacyRollup()
    {
        ManufacturingCostInput input = new(
            IsTech3: false,
            IncludeInventionCosts: true,
            IncludeCopyCosts: true,
            UserRuns: 36,
            TotalInventedRuns: 108,
            PerInventionRunCost: 1_250.5,
            TotalCopyCost: 21_600,
            MainFacilityUsage: 180,
            ComponentUsage: 90,
            InventionUsage: 309.4,
            CopyUsage: 252,
            RemainingReactionUsage: 45,
            ReprocessingUsage: 12);

        ManufacturingCostCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.InventionCost.Should().Be(45_018d);
        result.Value.CopyCost.Should().Be(7_200d);
        result.Value.TotalUsage.Should().BeApproximately(888.4d, 0.000001d);
    }

    [Fact]
    public void Calculate_WhenTech3_ReturnsZeroCopyCost()
    {
        ManufacturingCostInput input = new(
            IsTech3: true,
            IncludeInventionCosts: true,
            IncludeCopyCosts: true,
            UserRuns: 10,
            TotalInventedRuns: 0,
            PerInventionRunCost: 500,
            TotalCopyCost: 9_000,
            MainFacilityUsage: 10,
            ComponentUsage: 20,
            InventionUsage: 30,
            CopyUsage: 0,
            RemainingReactionUsage: 0,
            ReprocessingUsage: 0);

        ManufacturingCostCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.CopyCost.Should().Be(0);
        result.Value.InventionCost.Should().Be(5_000d);
    }

    [Fact]
    public void Calculate_WhenCopyCostsNeedInventedRunsButNoneProvided_ReturnsFailure()
    {
        ManufacturingCostInput input = new(
            IsTech3: false,
            IncludeInventionCosts: false,
            IncludeCopyCosts: true,
            UserRuns: 1,
            TotalInventedRuns: 0,
            PerInventionRunCost: 0,
            TotalCopyCost: 100,
            MainFacilityUsage: 0,
            ComponentUsage: 0,
            InventionUsage: 0,
            CopyUsage: 0,
            RemainingReactionUsage: 0,
            ReprocessingUsage: 0);

        ManufacturingCostCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_TOTAL_INVENTED_RUNS");
    }

    [Fact]
    public void Calculate_WhenInventionCostsExcluded_ReturnsZeroInventionCost()
    {
        ManufacturingCostInput input = new(
            IsTech3: false,
            IncludeInventionCosts: false,
            IncludeCopyCosts: false,
            UserRuns: 25,
            TotalInventedRuns: 50,
            PerInventionRunCost: 999,
            TotalCopyCost: 10_000,
            MainFacilityUsage: 5,
            ComponentUsage: 4,
            InventionUsage: 3,
            CopyUsage: 2,
            RemainingReactionUsage: 1,
            ReprocessingUsage: 0.5);

        ManufacturingCostCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.InventionCost.Should().Be(0);
        result.Value.CopyCost.Should().Be(0);
        result.Value.TotalUsage.Should().BeApproximately(15.5d, 0.000001d);
    }
}