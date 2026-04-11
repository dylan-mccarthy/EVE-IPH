using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Infrastructure.ESI;

/// <summary>
/// Represents the token state returned by EVE SSO.
/// </summary>
public sealed record EsiAccessToken(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyList<string> Scopes,
    Maybe<CharacterId> CharacterId)
{
    public EsiTokenRecord ToRecord() => new(AccessToken, RefreshToken, ExpiresAtUtc, Scopes, CharacterId);

    public static EsiAccessToken FromRecord(EsiTokenRecord record) =>
        new(record.AccessToken, record.RefreshToken, record.ExpiresAtUtc, record.Scopes, record.CharacterId);
}