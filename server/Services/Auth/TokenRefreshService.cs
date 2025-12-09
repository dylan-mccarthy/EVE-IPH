using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using server.Infrastructure;

namespace server.Services.Auth;

public sealed class TokenRefreshService : ITokenRefreshService
{
    private readonly HttpClient _http;
    private readonly ITokenStore _tokenStore;
    private readonly EveSsoOptions _ssoOptions;
    private readonly ILogger<TokenRefreshService> _logger;

    public TokenRefreshService(
        HttpClient http,
        ITokenStore tokenStore,
        IOptions<EveSsoOptions> ssoOptions,
        ILogger<TokenRefreshService> logger)
    {
        _http = http;
        _tokenStore = tokenStore;
        _ssoOptions = ssoOptions.Value;
        _logger = logger;
    }

    public async Task<RefreshResult> RefreshTokenIfNeededAsync(long characterId, CancellationToken ct = default)
    {
        // Get current token from database
        var storedToken = await _tokenStore.GetTokenAsync(characterId, ct);
        
        if (storedToken is null)
        {
            _logger.LogWarning("No token found for character {CharacterId}", characterId);
            return new RefreshResult(false, null, null, "Token not found");
        }

        // Check if token needs refresh (refresh 5 minutes before expiry)
        var now = DateTimeOffset.UtcNow;
        var refreshThreshold = storedToken.ExpiresAt.AddMinutes(-5);

        if (now < refreshThreshold)
        {
            // Token is still valid
            _logger.LogDebug("Token for character {CharacterId} is still valid until {ExpiresAt}", 
                characterId, storedToken.ExpiresAt);
            return new RefreshResult(true, storedToken.AccessToken, storedToken.ExpiresAt, null);
        }

        // Token needs refresh
        _logger.LogInformation("Refreshing token for character {CharacterId}", characterId);

        try
        {
            var tokenResponse = await RefreshAccessTokenAsync(storedToken.RefreshToken, ct);
            
            if (tokenResponse is null)
            {
                return new RefreshResult(false, null, null, "Failed to refresh token");
            }

            // Calculate new expiry
            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.expires_in);

            // Update database
            await _tokenStore.UpdateTokenAsync(characterId, tokenResponse.access_token, expiresAt, ct);

            return new RefreshResult(true, tokenResponse.access_token, expiresAt, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token for character {CharacterId}", characterId);
            return new RefreshResult(false, null, null, ex.Message);
        }
    }

    private async Task<TokenResponse?> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_ssoOptions.Authority}/v2/oauth/token");

        // Use Basic auth if we have a client secret
        if (!string.IsNullOrEmpty(_ssoOptions.ClientSecret))
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_ssoOptions.ClientId}:{_ssoOptions.ClientSecret}")
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        var payload = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        };

        // Only include client_id if no secret
        if (string.IsNullOrEmpty(_ssoOptions.ClientSecret))
        {
            payload["client_id"] = _ssoOptions.ClientId;
        }

        request.Content = new FormUrlEncodedContent(payload);

        var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Token refresh failed: {StatusCode} - {Error}", response.StatusCode, error);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<TokenResponse>(json);
    }

    private sealed record TokenResponse(string access_token, int expires_in, string token_type, string refresh_token);
}
