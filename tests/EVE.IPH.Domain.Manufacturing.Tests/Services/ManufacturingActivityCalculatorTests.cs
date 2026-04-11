using EVE.IPH.Domain.Manufacturing.Models;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class ManufacturingActivityCalculatorTests
{
    [Fact]
    public void Calculate_WithCopyAndInventionSettings_ReturnsLegacyUsageAndTimeValues()
    {
        ManufacturingActivityInput input = new(
            IsTech3: false,
            IncludeCopyUsage: true,
            IncludeInventionUsage: true,
            IncludeCopyTime: true,
            IncludeInventionTime: true,
            TotalInventedRuns: 108,
            NumberOfInventionJobs: 18,
            NumberOfInventionSessions: 2,
            UserCopyRuns: 18,
            EstimatedItemValue: 2_500_000,
            CopyCostIndex: 0.04,
            CopyFwCostBonus: 0.8,
            CopyFacilityCostMultiplier: 0.9,
            CopyFacilityTaxRate: 0.05,
            InventionCostIndex: 0.06,
            InventionFwCostBonus: 0.7,
            InventionFacilityCostMultiplier: 0.85,
            InventionFacilityTaxRate: 0.04,
            BaseCopyTimeSeconds: 1_200,
            BaseInventionTimeSeconds: 7_200,
            ScienceSkillLevel: 5,
            AdvancedIndustrySkillLevel: 5,
            CopyFacilityTimeMultiplier: 0.75,
            InventionFacilityTimeMultiplier: 0.8,
            CopyImplantBonus: 0.02);

        ManufacturingActivityCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.CopyUsagePerRun.Should().BeApproximately(252d, 0.000001d);
        result.Value.InventionUsagePerRun.Should().BeApproximately(309.4d, 0.000001d);
        result.Value.CopyTimeSeconds.Should().BeApproximately(10_120.95d, 0.000001d);
        result.Value.InventionTimeSeconds.Should().BeApproximately(9_792d, 0.000001d);
    }

    [Fact]
    public void Calculate_Tech3DisablesCopyUsageAndUsesFixedInventionTime()
    {
        ManufacturingActivityInput input = new(
            IsTech3: true,
            IncludeCopyUsage: true,
            IncludeInventionUsage: true,
            IncludeCopyTime: true,
            IncludeInventionTime: true,
            TotalInventedRuns: 30,
            NumberOfInventionJobs: 6,
            NumberOfInventionSessions: 3,
            UserCopyRuns: 6,
            EstimatedItemValue: 1_000_000,
            CopyCostIndex: 0.05,
            CopyFwCostBonus: 1,
            CopyFacilityCostMultiplier: 1,
            CopyFacilityTaxRate: 0.05,
            InventionCostIndex: 0.03,
            InventionFwCostBonus: 1,
            InventionFacilityCostMultiplier: 1,
            InventionFacilityTaxRate: 0.05,
            BaseCopyTimeSeconds: 1_000,
            BaseInventionTimeSeconds: 9_000,
            ScienceSkillLevel: 4,
            AdvancedIndustrySkillLevel: 4,
            CopyFacilityTimeMultiplier: 1,
            InventionFacilityTimeMultiplier: 0.7,
            CopyImplantBonus: 0.01);

        ManufacturingActivityCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.CopyUsagePerRun.Should().Be(0);
        result.Value.CopyTimeSeconds.Should().Be(0);
        result.Value.InventionTimeSeconds.Should().Be(10_800d);
    }

    [Fact]
    public void Calculate_WhenUsageIncludedWithoutInventedRuns_ReturnsFailure()
    {
        ManufacturingActivityInput input = new(
            IsTech3: false,
            IncludeCopyUsage: true,
            IncludeInventionUsage: false,
            IncludeCopyTime: false,
            IncludeInventionTime: false,
            TotalInventedRuns: 0,
            NumberOfInventionJobs: 1,
            NumberOfInventionSessions: 0,
            UserCopyRuns: 0,
            EstimatedItemValue: 1,
            CopyCostIndex: 0.01,
            CopyFwCostBonus: 1,
            CopyFacilityCostMultiplier: 1,
            CopyFacilityTaxRate: 0,
            InventionCostIndex: 0.01,
            InventionFwCostBonus: 1,
            InventionFacilityCostMultiplier: 1,
            InventionFacilityTaxRate: 0,
            BaseCopyTimeSeconds: 1,
            BaseInventionTimeSeconds: 1,
            ScienceSkillLevel: 0,
            AdvancedIndustrySkillLevel: 0,
            CopyFacilityTimeMultiplier: 1,
            InventionFacilityTimeMultiplier: 1,
            CopyImplantBonus: 0);

        ManufacturingActivityCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_TOTAL_INVENTED_RUNS");
    }

    [Fact]
    public void Calculate_WhenInventionTimeExcluded_ReturnsZeroInventionTime()
    {
        ManufacturingActivityInput input = new(
            IsTech3: false,
            IncludeCopyUsage: false,
            IncludeInventionUsage: false,
            IncludeCopyTime: false,
            IncludeInventionTime: false,
            TotalInventedRuns: 1,
            NumberOfInventionJobs: 0,
            NumberOfInventionSessions: 2,
            UserCopyRuns: 0,
            EstimatedItemValue: 1,
            CopyCostIndex: 0,
            CopyFwCostBonus: 1,
            CopyFacilityCostMultiplier: 1,
            CopyFacilityTaxRate: 0,
            InventionCostIndex: 0,
            InventionFwCostBonus: 1,
            InventionFacilityCostMultiplier: 1,
            InventionFacilityTaxRate: 0,
            BaseCopyTimeSeconds: 1,
            BaseInventionTimeSeconds: 1,
            ScienceSkillLevel: 0,
            AdvancedIndustrySkillLevel: 0,
            CopyFacilityTimeMultiplier: 1,
            InventionFacilityTimeMultiplier: 1,
            CopyImplantBonus: 0);

        ManufacturingActivityCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.InventionTimeSeconds.Should().Be(0);
    }
}