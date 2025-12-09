using server.Models;

namespace server.Services.Auth;

public interface ITokenStore
{
    Task SaveTokenAsync(long characterId, string accessToken, DateTimeOffset expiresAt, string refreshToken, string scopes, CancellationToken ct = default);
    Task<StoredToken?> GetTokenAsync(long characterId, CancellationToken ct = default);
    Task UpdateTokenAsync(long characterId, string accessToken, DateTimeOffset expiresAt, CancellationToken ct = default);
}

public sealed record StoredToken(long CharacterId, string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken, string Scopes);
