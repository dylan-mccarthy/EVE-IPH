using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Persists ESI SSO token state for the active account.
/// </summary>
public interface IEsiTokenStore
{
    Task<Maybe<EsiTokenRecord>> ReadAsync(CancellationToken cancellationToken = default);

    Task<Maybe<EsiTokenRecord>> ReadAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<EsiTokenRecord>> WriteAsync(EsiTokenRecord token, CancellationToken cancellationToken = default);

    Task<Result<bool>> ClearAsync(CancellationToken cancellationToken = default);

    Task<Result<bool>> ClearAsync(CharacterId characterId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Persisted ESI SSO token state.
/// </summary>
public sealed record EsiTokenRecord(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyList<string> Scopes,
    Maybe<CharacterId> CharacterId);