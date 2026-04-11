using EVE.IPH.Domain.Manufacturing.Models;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class InventionCalculatorTests
{
    [Fact]
    public void Calculate_WithCharacterSkills_ReturnsLegacyInventionPlan()
    {
        InventionPlanInput input = new(
            UserRuns: 100,
            MaxProductionLimit: 10,
            BaseInventionChance: 0.34,
            EncryptionSkillLevel: 4,
            SupportingSkillLevels: [4, 4],
            Decryptor: new InventionDecryptorModifier(RunModifier: 2, ProbabilityModifier: 1.1),
            NumberOfLaboratoryLines: 10,
            SingleInventionMaterialsCost: 1_200_000,
            UseTypicalSkills: false);

        InventionCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.InventionChance.Should().BeApproximately(0.5111333333333334d, 0.0000001d);
        result.Value.SingleInventedBlueprintRuns.Should().Be(12);
        result.Value.RequiredBlueprintCopies.Should().Be(9);
        result.Value.NumberOfInventionJobs.Should().Be(18);
        result.Value.NumberOfInventionSessions.Should().Be(2);
        result.Value.TotalInventedRuns.Should().Be(108);
        result.Value.PerInventionRunCost.Should().BeApproximately(195643.66766662314d, 0.000001d);
    }

    [Fact]
    public void Calculate_WithTypicalSkills_UsesLegacyTypicalSkillFormula()
    {
        InventionPlanInput input = new(
            UserRuns: 50,
            MaxProductionLimit: 1,
            BaseInventionChance: 0.26,
            EncryptionSkillLevel: 1,
            SupportingSkillLevels: [1, 1],
            Decryptor: new InventionDecryptorModifier(RunModifier: 4, ProbabilityModifier: 0.9),
            NumberOfLaboratoryLines: 5,
            SingleInventionMaterialsCost: 500_000,
            UseTypicalSkills: true);

        InventionCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.InventionChance.Should().BeApproximately(0.3198d, 0.0000001d);
        result.Value.SingleInventedBlueprintRuns.Should().Be(5);
        result.Value.RequiredBlueprintCopies.Should().Be(10);
        result.Value.NumberOfInventionJobs.Should().Be(32);
        result.Value.NumberOfInventionSessions.Should().Be(7);
        result.Value.TotalInventedRuns.Should().Be(50);
    }

    [Fact]
    public void Calculate_WhenBaseChanceIsInvalid_ReturnsFailure()
    {
        InventionPlanInput input = new(
            UserRuns: 10,
            MaxProductionLimit: 1,
            BaseInventionChance: 0,
            EncryptionSkillLevel: 4,
            SupportingSkillLevels: [4, 4],
            Decryptor: new InventionDecryptorModifier(RunModifier: 0, ProbabilityModifier: 1),
            NumberOfLaboratoryLines: 1,
            SingleInventionMaterialsCost: 1_000);

        InventionCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_BASE_INVENTION_CHANCE");
    }

    [Fact]
    public void Calculate_WhenRunModifierMakesInventedRunsInvalid_ReturnsFailure()
    {
        InventionPlanInput input = new(
            UserRuns: 10,
            MaxProductionLimit: 1,
            BaseInventionChance: 0.2,
            EncryptionSkillLevel: 4,
            SupportingSkillLevels: [4, 4],
            Decryptor: new InventionDecryptorModifier(RunModifier: -1, ProbabilityModifier: 1),
            NumberOfLaboratoryLines: 1,
            SingleInventionMaterialsCost: 1_000);

        InventionCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_INVENTED_RUNS");
    }
}