using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteCharacterStandingRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly ICharacterStandingRepository _sut;

    public SqliteCharacterStandingRepositoryTests()
    {
        _sut = new SqliteCharacterStandingRepository(_fixture.ConnectionFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ReplaceAsync_ReplacesCharacterStandings()
    {
        CharacterId characterId = new(100_000_301);

        await _sut.ReplaceAsync(characterId,
        [
            new CharacterStandingRecord(characterId, 500001, "Faction", "Amarr Empire", 2.5),
        ]);

        var result = await _sut.ReplaceAsync(characterId,
        [
            new CharacterStandingRecord(characterId, 500002, "Corporation", "Caldari Navy", 6.7),
            new CharacterStandingRecord(characterId, 3008416, "Agent", "Important Agent", -1.2),
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(standing => standing.NpcId == 500002 && standing.NpcName == "Caldari Navy");
        result.Value.Should().NotContain(standing => standing.NpcId == 500001);
    }

    [Fact]
    public async Task GetByCharacterIdAsync_AfterReplace_ReturnsStoredStandings()
    {
        CharacterId characterId = new(100_000_302);

        await _sut.ReplaceAsync(characterId,
        [
            new CharacterStandingRecord(characterId, 500001, "Faction", "Amarr Empire", 2.5),
            new CharacterStandingRecord(characterId, 500002, "Corporation", "Caldari Navy", 6.7),
        ]);

        var result = await _sut.GetByCharacterIdAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(standing => standing.NpcType == "Faction" && standing.NpcName == "Amarr Empire");
    }
}