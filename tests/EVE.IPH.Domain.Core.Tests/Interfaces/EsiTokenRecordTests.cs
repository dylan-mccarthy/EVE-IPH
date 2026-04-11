using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Tests.Interfaces;

public sealed class EsiTokenRecordTests
{
    [Fact]
    public void Record_WithCharacterId_PreservesValues()
    {
        DateTimeOffset expiresAtUtc = new(2026, 4, 11, 12, 30, 0, TimeSpan.Zero);

        EsiTokenRecord record = new(
            "access-token",
            "refresh-token",
            expiresAtUtc,
            ["esi-skills.read_skills", "esi-characters.read_standings"],
            Maybe<CharacterId>.Some(new CharacterId(90000001)));

        record.AccessToken.Should().Be("access-token");
        record.RefreshToken.Should().Be("refresh-token");
        record.ExpiresAtUtc.Should().Be(expiresAtUtc);
        record.Scopes.Should().Contain("esi-skills.read_skills");
        record.CharacterId.HasValue.Should().BeTrue();
        record.CharacterId.Value.Value.Should().Be(90000001);
    }
}