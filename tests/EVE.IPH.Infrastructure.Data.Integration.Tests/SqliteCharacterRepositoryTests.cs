using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteCharacterRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly ICharacterRepository _sut;

    public SqliteCharacterRepositoryTests()
    {
        _sut = new SqliteCharacterRepository(_fixture.ConnectionFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task UpsertAsync_NewCharacter_InsertsAndReturnsRecord()
    {
        CharacterRecord character = new(
            new CharacterId(100_000_001),
            "Test Pilot",
            new CorporationId(200_000_001),
            Maybe<AllianceId>.None,
            IsDefault: false);

        Result<CharacterRecord> result = await _sut.UpsertAsync(character);

        result.IsSuccess.Should().BeTrue();
        result.Value.CharacterId.Value.Should().Be(100_000_001);
        result.Value.Name.Should().Be("Test Pilot");
    }

    [Fact]
    public async Task GetByIdAsync_AfterInsert_ReturnsCharacter()
    {
        CharacterRecord character = new(
            new CharacterId(100_000_002),
            "Alpha Pilot",
            new CorporationId(200_000_002),
            Maybe<AllianceId>.None,
            IsDefault: false);

        await _sut.UpsertAsync(character);

        Maybe<CharacterRecord> result = await _sut.GetByIdAsync(new CharacterId(100_000_002));

        result.HasValue.Should().BeTrue();
        result.Value.Name.Should().Be("Alpha Pilot");
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNone()
    {
        Maybe<CharacterRecord> result = await _sut.GetByIdAsync(new CharacterId(999_999_999));

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task UpsertAsync_ExistingCharacter_UpdatesRecord()
    {
        CharacterId id = new(100_000_003);
        CharacterRecord original = new(id, "Old Name", new CorporationId(200_000_003), Maybe<AllianceId>.None, false);
        CharacterRecord updated = new(id, "New Name", new CorporationId(200_000_003), Maybe<AllianceId>.None, true);

        await _sut.UpsertAsync(original);
        await _sut.UpsertAsync(updated);

        Maybe<CharacterRecord> result = await _sut.GetByIdAsync(id);

        result.HasValue.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
        result.Value.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ExistingCharacter_ReturnsTrue()
    {
        CharacterId id = new(100_000_004);
        await _sut.UpsertAsync(new CharacterRecord(id, "Delete Me", new CorporationId(1), Maybe<AllianceId>.None, false));

        Result<bool> result = await _sut.DeleteAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        Result<bool> result = await _sut.DeleteAsync(new CharacterId(888_888_888));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllInsertedCharacters()
    {
        CharacterRecord a = new(new CharacterId(100_000_010), "Zara", new CorporationId(1), Maybe<AllianceId>.None, false);
        CharacterRecord b = new(new CharacterId(100_000_011), "Aaron", new CorporationId(1), Maybe<AllianceId>.None, false);

        await _sut.UpsertAsync(a);
        await _sut.UpsertAsync(b);

        Result<IReadOnlyList<CharacterRecord>> result = await _sut.GetAllAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(r => r.CharacterId.Value == 100_000_010);
        result.Value.Should().Contain(r => r.CharacterId.Value == 100_000_011);
    }
}
