using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Characters.Services;
using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Characters.Tests.Services;

public sealed class SkillServiceTests
{
    [Fact]
    public void GetSkill_WhenSkillExists_ReturnsSkill()
    {
        SkillService service = new();

        var result = service.GetSkill([CreateSkill()], new TypeId(3359));

        result.HasValue.Should().BeTrue();
        result.Value.Name.Should().Be("Connections");
    }

    [Fact]
    public void GetSkill_WhenSkillDoesNotExist_ReturnsNone()
    {
        SkillService service = new();

        var result = service.GetSkill([], new TypeId(3359));

        result.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void GetSkillLevel_WhenOverrideIsSet_ReturnsOverrideLevel()
    {
        SkillService service = new();
        Skill skill = CreateSkill(trainedLevel: 3, activeLevel: 4, isOverridden: true, overrideLevel: 5);

        int result = service.GetSkillLevel([skill], skill.SkillTypeId, useActiveSkillLevels: false);

        result.Should().Be(5);
    }

    [Fact]
    public void GetSkillLevel_WhenUsingActiveSkillLevels_ReturnsActiveLevel()
    {
        SkillService service = new();
        Skill skill = CreateSkill(trainedLevel: 3, activeLevel: 4);

        int result = service.GetSkillLevel([skill], skill.SkillTypeId, useActiveSkillLevels: true);

        result.Should().Be(4);
    }

    [Fact]
    public void GetSkillLevel_WhenSkillIsMissing_ReturnsZero()
    {
        SkillService service = new();

        int result = service.GetSkillLevel([], new TypeId(3359), useActiveSkillLevels: false);

        result.Should().Be(0);
    }

    private static Skill CreateSkill(
        int trainedLevel = 3,
        int activeLevel = 3,
        bool isOverridden = false,
        int overrideLevel = 0) =>
        new(new TypeId(3359), "Connections", trainedLevel, activeLevel, 8000, isOverridden, overrideLevel);
}