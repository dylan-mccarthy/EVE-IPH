using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteCharacterResearchAgentRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly ICharacterResearchAgentRepository _sut;

    public SqliteCharacterResearchAgentRepositoryTests()
    {
        _sut = new SqliteCharacterResearchAgentRepository(_fixture.ConnectionFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ReplaceAsync_AndGetByCharacterIdAsync_PersistResearchAgents()
    {
        CharacterId characterId = new(100_000_601);
        DateTimeOffset startedAt = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

        await _sut.ReplaceAsync(characterId,
        [
            new CharacterResearchAgentRecord(characterId, 3019499, new TypeId(11452), 54.5, startedAt, 12.25),
        ]);

        var result = await _sut.GetByCharacterIdAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].AgentId.Should().Be(3019499);
        result.Value[0].ResearchStartDate.Should().Be(startedAt);
    }

    [Fact]
    public async Task ReplaceAsync_ReplacesExistingResearchAgentsForCharacter()
    {
        CharacterId characterId = new(100_000_602);

        await _sut.ReplaceAsync(characterId,
        [
            new CharacterResearchAgentRecord(characterId, 3019499, new TypeId(11452), 54.5, new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero), 12.25),
        ]);

        var result = await _sut.ReplaceAsync(characterId,
        [
            new CharacterResearchAgentRecord(characterId, 3019500, new TypeId(11453), 61.0, new DateTimeOffset(2026, 4, 2, 12, 0, 0, TimeSpan.Zero), 8.5),
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(agent => agent.AgentId == 3019500);
    }
}