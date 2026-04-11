using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Interfaces;
using NSubstitute;

namespace EVE.IPH.Infrastructure.ESI.Tests;

public sealed class EsiCharacterResearchAgentDataSourceTests
{
    [Fact]
    public async Task GetResearchAgentsAsync_MapsEsiPayloadToDomainSourceData()
    {
        IEsiClient esiClient = Substitute.For<IEsiClient>();
        CharacterId characterId = new(100_000_501);
        DateTimeOffset startedAt = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

        esiClient.GetResearchAgentsAsync(characterId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<EsiResearchAgent>>.Success([
                new EsiResearchAgent(3019499, new TypeId(11452), startedAt, 54.5, 12.25),
            ]));

        EsiCharacterResearchAgentDataSource sut = new(esiClient);

        var result = await sut.GetResearchAgentsAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].AgentId.Should().Be(3019499);
        result.Value[0].ResearchStartDate.Should().Be(startedAt);
    }
}