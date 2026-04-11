using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Characters.Services;
using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Characters.Tests.Services;

public sealed class ResearchAgentDatacoreServiceTests
{
    [Fact]
    public void BuildSummary_ComputesDatacoreNamesAndValues()
    {
        ResearchAgentDatacoreService service = new();
        ResearchAgent[] agents =
        [
            new(1001, new TypeId(2001), "Agent One", "Gallente Starship Engineering", 250.5, 55, 4, "Dodixie", DateTimeOffset.UtcNow.AddDays(-10), 0),
            new(1002, new TypeId(2002), "Agent Two", "Mechanical Engineering", 199.9, 42, 3, "Jita", DateTimeOffset.UtcNow.AddDays(-5), 0),
        ];
        Dictionary<string, double> prices = new()
        {
            ["Datacore - Gallentean Starship Engineering"] = 250000,
            ["Datacore - Mechanical Engineering"] = 180000,
        };

        ResearchAgentDatacoreSummary result = service.BuildSummary(agents, prices, 10000);

        result.Agents.Should().HaveCount(2);
        result.Agents[0].DatacoreName.Should().Be("Datacore - Gallentean Starship Engineering");
        result.Agents[0].CurrentDatacores.Should().Be(2);
        result.Agents[0].CurrentValue.Should().Be(480000);
        result.Agents[1].DatacoreName.Should().Be("Datacore - Mechanical Engineering");
        result.Agents[1].CurrentDatacores.Should().Be(1);
        result.TotalValue.Should().Be(650000);
    }

    [Fact]
    public void BuildSummary_UsesZeroValueWhenPriceMissing()
    {
        ResearchAgentDatacoreService service = new();
        ResearchAgent[] agents =
        [
            new(1001, new TypeId(2001), "Agent One", "Amarr Starship Engineering", 101, 20, 4, "Amarr", DateTimeOffset.UtcNow.AddDays(-10), 0),
        ];

        ResearchAgentDatacoreSummary result = service.BuildSummary(agents, new Dictionary<string, double>(), 10000);

        result.Agents.Should().ContainSingle();
        result.Agents[0].DatacoreName.Should().Be("Datacore - Amarian Starship Engineering");
        result.Agents[0].CurrentDatacores.Should().Be(1);
        result.Agents[0].CurrentValue.Should().Be(-10000);
        result.TotalValue.Should().Be(-10000);
    }
}