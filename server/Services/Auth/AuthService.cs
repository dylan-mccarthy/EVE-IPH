using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using server.Infrastructure;
using server.Models;
using server.Services.Characters;

namespace server.Services.Auth;

public sealed class AuthService : IAuthService
{
    private readonly EveSsoOptions _options;
    private readonly IHttpClientFactory _clients;
    private readonly ICharacterService _characters;
    private readonly ICharacterPersistenceService _characterPersistence;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<AuthService> _logger;
    private readonly ConcurrentDictionary<string, PendingAuth> _pending = new();

    public AuthService(IOptions<EveSsoOptions> options, IHttpClientFactory clients, ICharacterService characters, ICharacterPersistenceService characterPersistence, ITokenStore tokenStore, ILogger<AuthService> logger)
    {
        _options = options.Value;
        _clients = clients;
        _characters = characters;
        _characterPersistence = characterPersistence;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    public Task<AuthStartResponse> StartAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        var state = GenerateState();
        var codeVerifier = GenerateCodeVerifier();
        _pending[state] = new PendingAuth(DateTimeOffset.UtcNow.AddMinutes(10), codeVerifier);
        var url = BuildAuthorizeUrl(state, codeVerifier);
        return Task.FromResult(new AuthStartResponse(url, state));
    }

    public async Task<AuthExchangeResponse> ExchangeAsync(AuthExchangeRequest request, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Exchange requested for state: {State}", request.State);
        
        if (!_pending.TryRemove(request.State, out var pending) || pending.ExpiresAt < DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Invalid or expired state: {State}. Pending count: {Count}", request.State, _pending.Count);
            throw new InvalidOperationException("Invalid or expired state.");
        }

        _logger.LogInformation("State validated, exchanging code");
        var sso = _clients.CreateClient("sso");
        var token = await ExchangeCodeAsync(sso, request.Code, pending.CodeVerifier, ct);
        var verification = await VerifyAsync(sso, token.AccessToken, ct);

        var profile = await _characters.GetProfileAsync(verification.CharacterID, token.AccessToken, ct);
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);

        // Save character data to database first
        await _characterPersistence.SaveCharacterAsync(
            verification.CharacterID,
            verification.CharacterName,
            profile,
            verification.Scopes,
            ct);

        // Then save token to database
        await _tokenStore.SaveTokenAsync(
            verification.CharacterID, 
            token.AccessToken, 
            expiresAt, 
            token.RefreshToken, 
            verification.Scopes, 
            ct);

        _logger.LogInformation("Auth exchange completed for character {CharacterId} ({CharacterName}), character and token saved to database", 
            verification.CharacterID, verification.CharacterName);
        
        return new AuthExchangeResponse(verification.CharacterID, verification.CharacterName, token.AccessToken, expiresAt, token.RefreshToken, profile);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            _options.ClientId = EveSsoOptions.LegacyClientId;
        }
        if (string.IsNullOrWhiteSpace(_options.RedirectUri))
        {
            throw new InvalidOperationException("EVE SSO redirect URI is not configured. Set EveSso:RedirectUri in configuration.");
        }
    }

    private async Task<TokenResponse> ExchangeCodeAsync(HttpClient client, string code, string codeVerifier, CancellationToken ct)
    {
        _logger.LogInformation("Exchanging authorization code for access token. ClientId: {ClientId}", _options.ClientId);
        
        var formData = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "authorization_code"),
            new("code", code),
            new("code_verifier", codeVerifier)
        };

        // Only include client_id in body if NOT using client secret (public client)
        if (string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            _logger.LogInformation("Using public client (no secret)");
            formData.Add(new("client_id", _options.ClientId));
        }
        else
        {
            _logger.LogInformation("Using client secret authentication");
        }
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/v2/oauth/token")
        {
            Content = new FormUrlEncodedContent(formData)
        };

        if (!string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        using var response = await client.SendAsync(request, ct);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Token exchange failed with status {StatusCode}. Response: {Response}", 
                response.StatusCode, errorContent);
        }
        
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
        _logger.LogInformation("Successfully received access token");
        return token ?? throw new InvalidOperationException("No token returned from SSO.");
    }

    private async Task<VerifyResponse> VerifyAsync(HttpClient client, string accessToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/oauth/verify");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<VerifyResponse>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("No verification payload returned from SSO.");
    }

    private string BuildAuthorizeUrl(string state, string codeVerifier)
    {
        var codeChallenge = BuildCodeChallenge(codeVerifier);
        var query = new QueryStringBuilder()
            .Add("response_type", "code")
            .Add("redirect_uri", _options.RedirectUri)
            .Add("client_id", _options.ClientId)
            .Add("scope", _options.Scopes)
            .Add("state", state)
            .Add("code_challenge", codeChallenge)
            .Add("code_challenge_method", "S256")
            .Build();

        return $"{_options.Authority.TrimEnd('/')}/v2/oauth/authorize?{query}";
    }

    private static string GenerateState()
    {
        Span<byte> buffer = stackalloc byte[16];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToHexString(buffer);
    }

    private static string GenerateCodeVerifier()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        return Base64UrlEncode(buffer);
    }

    private static string BuildCodeChallenge(string codeVerifier)
    {
        using var sha = SHA256.Create();
        var hashed = sha.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hashed);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        var base64 = Convert.ToBase64String(bytes.ToArray());
        return base64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private sealed record PendingAuth(DateTimeOffset ExpiresAt, string CodeVerifier);

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string RefreshToken);

    private sealed record VerifyResponse(
        long CharacterID,
        string CharacterName,
        string CharacterOwnerHash,
        string TokenType,
        string Scopes,
        DateTime ExpiresOn);

    private sealed class QueryStringBuilder
    {
        private readonly List<string> _parts = new();

        public QueryStringBuilder Add(string key, string value)
        {
            _parts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
            return this;
        }

        public string Build() => string.Join('&', _parts);
    }
}
