using server.Models;

namespace server.Services.Auth;

public interface ITokenRefreshService
{
    Task<RefreshResult> RefreshTokenIfNeededAsync(long characterId, CancellationToken ct = default);
}

public sealed record RefreshResult(bool Success, string? AccessToken, DateTimeOffset? ExpiresAt, string? Error);
