using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Characters.Services;

namespace EVE.IPH.Domain.Characters.Tests.Services;

public sealed class StandingsServiceTests
{
    [Fact]
    public void GetStanding_ById_WhenStandingExists_ReturnsBaseStanding()
    {
        StandingsService service = new();

        double result = service.GetStanding([CreateStanding()], 1000169);

        result.Should().Be(3.25D);
    }

    [Fact]
    public void GetStanding_ByName_WhenStandingExists_ReturnsBaseStanding()
    {
        StandingsService service = new();

        double result = service.GetStanding([CreateStanding()], "Caldari Navy");

        result.Should().Be(3.25D);
    }

    [Fact]
    public void GetEffectiveStanding_WithPositiveStanding_UsesConnectionsFormula()
    {
        StandingsService service = new();

        double result = service.GetEffectiveStanding(3.25D, connectionsLevel: 4, diplomacyLevel: 0);

        result.Should().BeApproximately(4.33D, 0.01D);
    }

    [Fact]
    public void GetEffectiveStanding_WithNegativeStanding_UsesDiplomacyFormula()
    {
        StandingsService service = new();

        double result = service.GetEffectiveStanding(-2.5D, connectionsLevel: 0, diplomacyLevel: 3);

        result.Should().BeApproximately(-1.0D, 0.01D);
    }

    [Fact]
    public void GetEffectiveStanding_ById_WhenStandingIsMissing_ReturnsZero()
    {
        StandingsService service = new();

        double result = service.GetEffectiveStanding([], 1000169, connectionsLevel: 4, diplomacyLevel: 3);

        result.Should().Be(0D);
    }

    private static NpcStanding CreateStanding() => new(1000169, "corporation", "Caldari Navy", 3.25D);
}