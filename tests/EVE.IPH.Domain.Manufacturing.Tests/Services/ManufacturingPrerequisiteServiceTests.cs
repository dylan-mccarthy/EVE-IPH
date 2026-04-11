using EVE.IPH.Domain.Manufacturing.Models;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class ManufacturingPrerequisiteServiceTests
{
    private readonly ManufacturingPrerequisiteService _sut = new();

    [Fact]
    public void Calculate_WhenNoRequiredSkills_ReturnsBuildableWithNeutralMultiplier()
    {
        ManufacturingPrerequisiteInput input = new([], new Dictionary<long, int>());

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanBuild.Should().BeTrue();
        result.Value.AdvancedManufacturingTimeMultiplier.Should().Be(1d);
    }

    [Fact]
    public void Calculate_WhenCharacterHasRequiredSkills_ReturnsBuildableAndAppliesAdvancedBonuses()
    {
        ManufacturingPrerequisiteInput input = new(
        [
            new ManufacturingSkillRequirement(3398, 4),
            new ManufacturingSkillRequirement(81896, 3),
            new ManufacturingSkillRequirement(12345, 2),
        ],
        new Dictionary<long, int>
        {
            [3398] = 5,
            [81896] = 4,
            [12345] = 2,
        });

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanBuild.Should().BeTrue();
        result.Value.AdvancedManufacturingTimeMultiplier.Should().BeApproximately(0.874d, 0.000001d);
    }

    [Fact]
    public void Calculate_WhenCharacterIsMissingRequiredSkill_ReturnsNotBuildable()
    {
        ManufacturingPrerequisiteInput input = new(
        [
            new ManufacturingSkillRequirement(3398, 4),
            new ManufacturingSkillRequirement(11444, 2),
        ],
        new Dictionary<long, int>
        {
            [3398] = 4,
        });

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanBuild.Should().BeFalse();
        result.Value.AdvancedManufacturingTimeMultiplier.Should().BeApproximately(0.96d, 0.000001d);
    }

    [Fact]
    public void Calculate_WhenRequiredSkillLevelIsInvalid_ReturnsFailure()
    {
        ManufacturingPrerequisiteInput input = new(
        [
            new ManufacturingSkillRequirement(3398, 0),
        ],
        new Dictionary<long, int>());

        var result = _sut.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_REQUIRED_SKILL_LEVEL");
    }
}