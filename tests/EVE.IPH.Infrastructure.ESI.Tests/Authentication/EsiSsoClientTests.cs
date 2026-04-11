using System.Net;
using System.Text;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Authentication;

namespace EVE.IPH.Infrastructure.ESI.Tests.Authentication;

public sealed class EsiSsoClientTests
{
    [Fact]
    public void CreateAuthorizationRequest_BuildsPkceAuthorizationUri()
    {
        EsiSsoClient client = CreateClient("{}");

        EsiAuthorizationRequest request = client.CreateAuthorizationRequest([
            "esi-skills.read_skills",
            "esi-characters.read_standings"
        ]);

        request.AuthorizationUri.ToString().Should().Contain("response_type=code");
        request.AuthorizationUri.ToString().Should().Contain("code_challenge_method=S256");
        request.AuthorizationUri.ToString().Should().Contain("client_id=client-id");
        request.State.Should().NotBeNullOrWhiteSpace();
        request.CodeVerifier.Should().NotBeNullOrWhiteSpace();
        request.CodeChallenge.Should().NotBeNullOrWhiteSpace();
        request.Scopes.Should().BeEquivalentTo(["esi-skills.read_skills", "esi-characters.read_standings"]);
    }

    [Fact]
    public async Task ExchangeAuthorizationCodeAsync_ParsesJwtAndPersistsToken()
    {
        string jwt = CreateJwtPayload("Character:EVE:90000001", ["esi-skills.read_skills"], DateTimeOffset.UtcNow.AddMinutes(30));
        string payload = $$"""
            {
              "access_token": "{{jwt}}",
              "refresh_token": "refresh-token",
              "expires_in": 1200
            }
            """;

        InMemoryTokenStore tokenStore = new();
        EsiSsoClient client = CreateClient(payload, tokenStore);

        var result = await client.ExchangeAuthorizationCodeAsync("auth-code", "verifier");

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.CharacterId.HasValue.Should().BeTrue();
        result.Value.CharacterId.Value.Value.Should().Be(90000001);
        tokenStore.StoredToken.HasValue.Should().BeTrue();
        tokenStore.StoredToken.Value.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task RefreshAccessTokenAsync_OnFailureStatus_ReturnsFailure()
    {
        HttpClient httpClient = new(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("bad refresh", Encoding.UTF8, "text/plain")
        }))
        {
            BaseAddress = new Uri("https://login.eveonline.com/")
        };

        EsiSsoClient client = new(httpClient, new InMemoryTokenStore(), CreateOptions(), TimeProvider.System);

        var result = await client.RefreshAccessTokenAsync("refresh-token");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ESI_TOKEN_400");
    }

    private static EsiSsoClient CreateClient(string payload, InMemoryTokenStore? tokenStore = null)
    {
        HttpClient httpClient = new(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        }))
        {
            BaseAddress = new Uri("https://login.eveonline.com/")
        };

        return new EsiSsoClient(httpClient, tokenStore ?? new InMemoryTokenStore(), CreateOptions(), TimeProvider.System);
    }

    private static EsiSsoOptions CreateOptions() =>
        new("client-id", "http://127.0.0.1:12500", "https://login.eveonline.com/v2/oauth/authorize", "https://login.eveonline.com/v2/oauth/token");

    private static string CreateJwtPayload(string subject, IReadOnlyList<string> scopes, DateTimeOffset expiresAtUtc)
    {
        string header = Base64UrlEncode("""{"alg":"RS256","typ":"JWT"}""");
        string payload = Base64UrlEncode($$"""
            {"sub":"{{subject}}","scp":["{{string.Join("\",\"", scopes)}}"],"exp":{{expiresAtUtc.ToUnixTimeSeconds()}}}
            """);

        return $"{header}.{payload}.signature";
    }

    private static string Base64UrlEncode(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class InMemoryTokenStore : IEsiTokenStore
    {
        public Maybe<EsiTokenRecord> StoredToken { get; private set; } = Maybe<EsiTokenRecord>.None;

        public Task<Result<bool>> ClearAsync(CancellationToken cancellationToken = default)
        {
            StoredToken = Maybe<EsiTokenRecord>.None;
            return Task.FromResult(Result<bool>.Success(true));
        }

        public Task<Maybe<EsiTokenRecord>> ReadAsync(CancellationToken cancellationToken = default) => Task.FromResult(StoredToken);

        public Task<Result<EsiTokenRecord>> WriteAsync(EsiTokenRecord token, CancellationToken cancellationToken = default)
        {
            StoredToken = Maybe<EsiTokenRecord>.Some(token);
            return Task.FromResult(Result<EsiTokenRecord>.Success(token));
        }
    }
}