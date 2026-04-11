using FluentAssertions;
using EVE.IPH.Domain.Reprocessing.Models;
using EVE.IPH.Domain.Reprocessing.Services;

namespace EVE.IPH.Domain.Reprocessing.Tests.Services;

public sealed class ReprocessingCalculatorTests
{
    [Fact]
    public void Calculate_WhenGivenOreInputs_ReturnsLegacyStyleYieldAndRecoveredQuantity()
    {
        ReprocessingCalculationInput input = new(
            UnitsPerBatch: 100,
            TotalQuantity: 550,
            BaseMaterialQuantityPerBatch: 415,
            FacilityMaterialMultiplier: 0.5,
            ReprocessingSkillLevel: 5,
            ReprocessingEfficiencySkillLevel: 4,
            ProcessingSkillLevel: 3,
            ImplantBonus: 0.04d);

        ReprocessingCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.RefineBatches.Should().Be(5);
        result.Value.TotalYield.Should().BeApproximately(0.6845904d, 0.00000001d);
        result.Value.RecoveredMaterialQuantity.Should().Be(1420);
    }

    [Fact]
    public void Calculate_WhenGivenScrapInputs_UsesScrapFormulaOnly()
    {
        ReprocessingCalculationInput input = new(
            UnitsPerBatch: 1,
            TotalQuantity: 7,
            BaseMaterialQuantityPerBatch: 12,
            FacilityMaterialMultiplier: 0.5,
            ReprocessingSkillLevel: 5,
            ReprocessingEfficiencySkillLevel: 5,
            ProcessingSkillLevel: 4,
            ImplantBonus: 0.04d,
            IsScrapReprocessing: true,
            ScrapBaseYield: 0.35d);

        ReprocessingCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.RefineBatches.Should().Be(7);
        result.Value.TotalYield.Should().BeApproximately(0.378d, 0.00000001d);
        result.Value.RecoveredMaterialQuantity.Should().Be(31);
    }

    [Fact]
    public void Calculate_WhenYieldWouldExceedOne_CapsAtOneHundredPercent()
    {
        ReprocessingCalculationInput input = new(
            UnitsPerBatch: 100,
            TotalQuantity: 200,
            BaseMaterialQuantityPerBatch: 50,
            FacilityMaterialMultiplier: 0.9,
            ReprocessingSkillLevel: 5,
            ReprocessingEfficiencySkillLevel: 5,
            ProcessingSkillLevel: 5,
            ImplantBonus: 0.04d);

        ReprocessingCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalYield.Should().Be(1d);
        result.Value.RecoveredMaterialQuantity.Should().Be(100);
    }

    [Fact]
    public void Calculate_WhenQuantityDoesNotReachOneBatch_ReturnsZeroOutputs()
    {
        ReprocessingCalculationInput input = new(
            UnitsPerBatch: 100,
            TotalQuantity: 99,
            BaseMaterialQuantityPerBatch: 50,
            FacilityMaterialMultiplier: 0.5,
            ReprocessingSkillLevel: 0,
            ReprocessingEfficiencySkillLevel: 0,
            ProcessingSkillLevel: 0,
            ImplantBonus: 0d);

        ReprocessingCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.RefineBatches.Should().Be(0);
        result.Value.TotalYield.Should().Be(0d);
        result.Value.RecoveredMaterialQuantity.Should().Be(0);
    }

    [Fact]
    public void Calculate_WhenUnitsPerBatchAreInvalid_ReturnsFailure()
    {
        ReprocessingCalculationInput input = new(
            UnitsPerBatch: 0,
            TotalQuantity: 100,
            BaseMaterialQuantityPerBatch: 10,
            FacilityMaterialMultiplier: 0.5,
            ReprocessingSkillLevel: 0,
            ReprocessingEfficiencySkillLevel: 0,
            ProcessingSkillLevel: 0,
            ImplantBonus: 0d);

        ReprocessingCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_UNITS_PER_BATCH");
    }
}