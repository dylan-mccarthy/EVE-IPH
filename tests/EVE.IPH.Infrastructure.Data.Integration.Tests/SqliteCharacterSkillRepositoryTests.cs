using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteCharacterSkillRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly ICharacterSkillRepository _sut;

    public SqliteCharacterSkillRepositoryTests()
    {
        _sut = new SqliteCharacterSkillRepository(_fixture.ConnectionFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task GetByCharacterIdAsync_AfterReplace_ReturnsStoredSkills()
    {
        CharacterId characterId = new(100_000_201);

        await _sut.ReplaceAsync(characterId,
        [
            new CharacterSkillRecord(characterId, new TypeId(3380), "Industry", 4, 5, 256000, false, 0),
            new CharacterSkillRecord(characterId, new TypeId(3402), "Science", 3, 3, 8000, true, 4),
        ]);

        var result = await _sut.GetByCharacterIdAsync(characterId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(skill => skill.SkillTypeId.Value == 3380 && !skill.IsOverridden);
        result.Value.Should().Contain(skill => skill.SkillTypeId.Value == 3402 && skill.IsOverridden && skill.OverrideLevel == 4);
    }

    [Fact]
    public async Task ReplaceAsync_PreservesExistingOverrideStateForMatchingSkills()
    {
        CharacterId characterId = new(100_000_202);

        await _sut.ReplaceAsync(characterId,
        [
            new CharacterSkillRecord(characterId, new TypeId(3380), "Industry", 3, 3, 12000, true, 5),
        ]);

        var result = await _sut.ReplaceAsync(characterId,
        [
            new CharacterSkillRecord(characterId, new TypeId(3380), "Industry", 4, 4, 24000, false, 0),
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].TrainedLevel.Should().Be(4);
        result.Value[0].ActiveLevel.Should().Be(4);
        result.Value[0].SkillPoints.Should().Be(24000);
        result.Value[0].IsOverridden.Should().BeTrue();
        result.Value[0].OverrideLevel.Should().Be(5);
    }

    [Fact]
    public async Task ReplaceAsync_KeepsOverrideOnlySkillsNotPresentInIncomingSnapshot()
    {
        CharacterId characterId = new(100_000_203);

        await _sut.ReplaceAsync(characterId,
        [
            new CharacterSkillRecord(characterId, new TypeId(3380), "Industry", 0, 0, 0, true, 4),
            new CharacterSkillRecord(characterId, new TypeId(3402), "Science", 3, 3, 8000, false, 0),
        ]);

        var result = await _sut.ReplaceAsync(characterId,
        [
            new CharacterSkillRecord(characterId, new TypeId(3402), "Science", 4, 4, 16000, false, 0),
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(skill => skill.SkillTypeId.Value == 3380 && skill.IsOverridden && skill.OverrideLevel == 4);
        result.Value.Should().Contain(skill => skill.SkillTypeId.Value == 3402 && skill.TrainedLevel == 4);
    }
}