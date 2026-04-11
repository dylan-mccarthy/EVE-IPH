using EVE.IPH.Domain.Manufacturing.Models;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class ManufacturingJobCalculatorTests
{
    [Fact]
    public void Calculate_WhenGivenLegacyStyleInputs_ReturnsRoundedMaterialsAndBatchedTime()
    {
        ManufacturingJobInput input = new(
            Runs: 10,
            BaseMaterialQuantity: 8,
            BlueprintMaterialEfficiencyPercent: 10,
            BlueprintTimeEfficiencyPercent: 20,
            BaseProductionTimeSeconds: 3600,
            AvailableBlueprints: 2,
            AvailableProductionLines: 1,
            IndustrySkillLevel: 5,
            AdvancedIndustrySkillLevel: 5,
            FacilityMaterialMultiplier: 0.99,
            FacilityTimeMultiplier: 0.7,
            ImplantTimeMultiplier: 0.96,
            SpecializedTimeMultiplier: 0.98);

        ManufacturingJobCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequiredMaterialQuantity.Should().Be(72);
        result.Value.JobsPerBatch.Should().Be(1);
        result.Value.FullJobSessions.Should().Be(10);
        result.Value.SingleRunDurationSeconds.Should().BeApproximately(1289.723904d, 0.00001d);
        result.Value.TotalJobDurationSeconds.Should().BeApproximately(12897.23904d, 0.00001d);
    }

    [Fact]
    public void Calculate_WhenRoundedMaterialUsageFallsBelowRuns_UsesRunsAsMinimumQuantity()
    {
        ManufacturingJobInput input = new(
            Runs: 5,
            BaseMaterialQuantity: 1,
            BlueprintMaterialEfficiencyPercent: 99,
            BlueprintTimeEfficiencyPercent: 0,
            BaseProductionTimeSeconds: 60,
            AvailableBlueprints: 3,
            AvailableProductionLines: 3,
            IndustrySkillLevel: 0,
            AdvancedIndustrySkillLevel: 0,
            FacilityMaterialMultiplier: 1.0,
            FacilityTimeMultiplier: 1.0);

        ManufacturingJobCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequiredMaterialQuantity.Should().Be(5);
        result.Value.JobsPerBatch.Should().Be(3);
        result.Value.FullJobSessions.Should().Be(2);
        result.Value.TotalJobDurationSeconds.Should().Be(120d);
    }

    [Fact]
    public void Calculate_WhenFulcrumBonusApplies_OverridesFacilityMaterialMultiplier()
    {
        ManufacturingJobInput input = new(
            Runs: 10,
            BaseMaterialQuantity: 10,
            BlueprintMaterialEfficiencyPercent: 0,
            BlueprintTimeEfficiencyPercent: 0,
            BaseProductionTimeSeconds: 100,
            AvailableBlueprints: 1,
            AvailableProductionLines: 1,
            IndustrySkillLevel: 0,
            AdvancedIndustrySkillLevel: 0,
            FacilityMaterialMultiplier: 0.5,
            FacilityTimeMultiplier: 1.0,
            HasFulcrumMaterialBonus: true);

        ManufacturingJobCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequiredMaterialQuantity.Should().Be(94);
    }

    [Fact]
    public void Calculate_WhenRunsAreInvalid_ReturnsFailure()
    {
        ManufacturingJobInput input = new(
            Runs: 0,
            BaseMaterialQuantity: 1,
            BlueprintMaterialEfficiencyPercent: 0,
            BlueprintTimeEfficiencyPercent: 0,
            BaseProductionTimeSeconds: 1,
            AvailableBlueprints: 1,
            AvailableProductionLines: 1,
            IndustrySkillLevel: 0,
            AdvancedIndustrySkillLevel: 0,
            FacilityMaterialMultiplier: 1.0,
            FacilityTimeMultiplier: 1.0);

        ManufacturingJobCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_RUNS");
    }
}