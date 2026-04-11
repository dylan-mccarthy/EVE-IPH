using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Interfaces;

namespace EVE.IPH.Infrastructure.ESI.Authentication;

/// <summary>
/// Handles PKCE authorization URL generation and token exchange with EVE SSO.
/// </summary>
public sealed class EsiSsoClient(HttpClient httpClient, IEsiTokenStore tokenStore, EsiSsoOptions options, TimeProvider timeProvider) : IEsiSsoClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly IEsiTokenStore _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    private readonly EsiSsoOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly EsiPkceGenerator _pkceGenerator = new();

    public EsiAuthorizationRequest CreateAuthorizationRequest(IEnumerable<string> scopes)
    {
        ArgumentNullException.ThrowIfNull(scopes);

        EsiPkceChallenge pkce = _pkceGenerator.Generate();
        string state = Guid.NewGuid().ToString("N");
        string joinedScopes = string.Join(' ', scopes.Where(scope => !string.IsNullOrWhiteSpace(scope)).Distinct(StringComparer.Ordinal));

        Uri uri = new($"{_options.AuthorizationEndpoint}?client_id={Uri.EscapeDataString(_options.ClientId)}&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}&response_type=code&scope={Uri.EscapeDataString(joinedScopes)}&state={state}&code_challenge={pkce.Challenge}&code_challenge_method=S256");

        return new EsiAuthorizationRequest(uri, state, pkce.Verifier, pkce.Challenge, joinedScopes.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public async Task<Result<EsiAccessToken>> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string codeVerifier,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationCode))
        {
            return Result<EsiAccessToken>.Failure("ESI_AUTH_CODE_REQUIRED", "Authorization code is required.");
        }

        if (string.IsNullOrWhiteSpace(codeVerifier))
        {
            return Result<EsiAccessToken>.Failure("ESI_CODE_VERIFIER_REQUIRED", "PKCE code verifier is required.");
        }

        Dictionary<string, string> values = new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = authorizationCode,
            ["client_id"] = _options.ClientId,
            ["code_verifier"] = codeVerifier,
        };

        return await ExchangeAsync(values, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<EsiAccessToken>> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<EsiAccessToken>.Failure("ESI_REFRESH_TOKEN_REQUIRED", "Refresh token is required.");
        }

        Dictionary<string, string> values = new()
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = _options.ClientId,
        };

        return await ExchangeAsync(values, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<EsiAccessToken>> ExchangeAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        try
        {
            using FormUrlEncodedContent content = new(values);
            using HttpResponseMessage response = await _httpClient.PostAsync(_options.TokenEndpoint, content, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                string message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return Result<EsiAccessToken>.Failure(
                    $"ESI_TOKEN_{(int)response.StatusCode}",
                    string.IsNullOrWhiteSpace(message)
                        ? $"Token exchange failed with status code {(int)response.StatusCode}."
                        : message);
            }

            TokenResponseDto? dto = await response.Content.ReadFromJsonAsync<TokenResponseDto>(SerializerOptions, cancellationToken).ConfigureAwait(false);
            if (dto is null)
            {
                return Result<EsiAccessToken>.Failure("ESI_TOKEN_EMPTY", "Token exchange returned an empty response.");
            }

            Result<EsiAccessToken> tokenResult = ParseAccessToken(dto);
            if (tokenResult.IsFailure)
            {
                return tokenResult;
            }

            EsiAccessToken token = tokenResult.Value;
            Result<EsiTokenRecord> writeResult = await _tokenStore.WriteAsync(token.ToRecord(), cancellationToken).ConfigureAwait(false);
            return writeResult.IsFailure
                ? Result<EsiAccessToken>.Failure(writeResult.Error)
                : Result<EsiAccessToken>.Success(token);
        }
        catch (HttpRequestException ex)
        {
            return Result<EsiAccessToken>.Failure("ESI_TOKEN_HTTP_ERROR", ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<EsiAccessToken>.Failure("ESI_TOKEN_TIMEOUT", ex.Message);
        }
        catch (JsonException ex)
        {
            return Result<EsiAccessToken>.Failure("ESI_TOKEN_INVALID_JSON", ex.Message);
        }
    }

    private Result<EsiAccessToken> ParseAccessToken(TokenResponseDto dto)
    {
        string[] segments = dto.AccessToken.Split('.');
        if (segments.Length < 2)
        {
            return Result<EsiAccessToken>.Failure("ESI_TOKEN_INVALID_JWT", "Access token is not a valid JWT.");
        }

        string payloadSegment = segments[1]
            .Replace('-', '+')
            .Replace('_', '/');

        int padding = 4 - (payloadSegment.Length % 4);
        if (padding is > 0 and < 4)
        {
            payloadSegment = payloadSegment.PadRight(payloadSegment.Length + padding, '=');
        }

        JwtPayloadDto? payload = JsonSerializer.Deserialize<JwtPayloadDto>(
            Encoding.UTF8.GetString(Convert.FromBase64String(payloadSegment)),
            SerializerOptions);

        if (payload is null)
        {
            return Result<EsiAccessToken>.Failure("ESI_TOKEN_INVALID_PAYLOAD", "Access token payload could not be parsed.");
        }

        Maybe<CharacterId> characterId = Maybe<CharacterId>.None;
        string[] subjectParts = payload.Subject?.Split(':', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (subjectParts.Length >= 3 && long.TryParse(subjectParts[^1], out long parsedCharacterId))
        {
            characterId = Maybe<CharacterId>.Some(new CharacterId(parsedCharacterId));
        }

        DateTimeOffset expiresAtUtc = payload.ExpiresAtUnixSeconds > 0
            ? DateTimeOffset.FromUnixTimeSeconds(payload.ExpiresAtUnixSeconds)
            : _timeProvider.GetUtcNow().AddSeconds(dto.ExpiresIn);

        IReadOnlyList<string> scopes = payload.Scopes?.Where(scope => !string.IsNullOrWhiteSpace(scope)).ToList() ?? [];

        return Result<EsiAccessToken>.Success(new EsiAccessToken(
            dto.AccessToken,
            dto.RefreshToken,
            expiresAtUtc,
            scopes,
            characterId));
    }

    private sealed record TokenResponseDto(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private sealed record JwtPayloadDto(
        [property: JsonPropertyName("sub")] string? Subject,
        [property: JsonPropertyName("scp")] IReadOnlyList<string>? Scopes,
        [property: JsonPropertyName("exp")] long ExpiresAtUnixSeconds);
}