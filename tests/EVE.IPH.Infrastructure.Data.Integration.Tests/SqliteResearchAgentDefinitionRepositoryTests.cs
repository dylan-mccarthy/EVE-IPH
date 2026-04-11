using Dapper;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteResearchAgentDefinitionRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly IResearchAgentDefinitionRepository _sut;

    public SqliteResearchAgentDefinitionRepositoryTests()
    {
        _sut = new SqliteResearchAgentDefinitionRepository(_fixture.ConnectionFactory);
        using System.Data.IDbConnection connection = _fixture.ConnectionFactory.CreateConnection();
        connection.Execute(
            "INSERT INTO RESEARCH_AGENTS (AGENT_ID, AGENT_NAME, RP_PER_DAY, LEVEL, STATION) VALUES (@AgentId, @AgentName, @PointsPerDay, @Level, @Station)",
            new[]
            {
                new { AgentId = 3019499L, AgentName = "Kikko Onimaro", PointsPerDay = 54.5, Level = 4, Station = "Jita IV - Moon 4" },
                new { AgentId = 3019500L, AgentName = "Lonetrek Agent", PointsPerDay = 61.0, Level = 4, Station = "Nonni I - Moon 1" },
            });
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task GetByAgentIdsAsync_ReturnsRequestedDefinitions()
    {
        var result = await _sut.GetByAgentIdsAsync([3019499L]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainKey(3019499L);
        result.Value[3019499L].AgentName.Should().Be("Kikko Onimaro");
        result.Value.Should().NotContainKey(3019500L);
    }
}