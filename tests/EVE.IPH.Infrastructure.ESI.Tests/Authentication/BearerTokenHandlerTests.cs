using System.Net;
using System.Net.Http.Headers;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Authentication;
using EVE.IPH.Infrastructure.ESI.Interfaces;

namespace EVE.IPH.Infrastructure.ESI.Tests.Authentication;

public sealed class BearerTokenHandlerTests
{
    [Fact]
    public async Task SendAsync_AddsBearerTokenToOutgoingRequest()
    {
        StubTokenProvider tokenProvider = new(
            Result<EsiAccessToken>.Success(CreateToken("access-token")),
            Result<EsiAccessToken>.Success(CreateToken("refresh-token")));

        RecordingHandler innerHandler = new(_ => new HttpResponseMessage(HttpStatusCode.OK));
        BearerTokenHandler handler = new(tokenProvider)
        {
            InnerHandler = innerHandler
        };

        using HttpMessageInvoker invoker = new(handler);

        using HttpResponseMessage response = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://esi.evetech.net/latest/characters/1/"),
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        innerHandler.Requests.Should().HaveCount(1);
        innerHandler.Requests[0].Headers.Authorization.Should().BeEquivalentTo(new AuthenticationHeaderValue("Bearer", "access-token"));
    }

    [Fact]
    public async Task SendAsync_WhenUnauthorized_RefreshesTokenAndRetriesOnce()
    {
        StubTokenProvider tokenProvider = new(
            Result<EsiAccessToken>.Success(CreateToken("expired-token")),
            Result<EsiAccessToken>.Success(CreateToken("fresh-token")));

        Queue<HttpResponseMessage> responses = new([
            new HttpResponseMessage(HttpStatusCode.Unauthorized),
            new HttpResponseMessage(HttpStatusCode.OK)
        ]);

        RecordingHandler innerHandler = new(_ => responses.Dequeue());
        BearerTokenHandler handler = new(tokenProvider)
        {
            InnerHandler = innerHandler
        };

        using HttpMessageInvoker invoker = new(handler);

        using HttpResponseMessage response = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://esi.evetech.net/latest/characters/1/skills/"),
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        tokenProvider.RefreshCallCount.Should().Be(1);
        innerHandler.Requests.Should().HaveCount(2);
        innerHandler.Requests[0].Headers.Authorization!.Parameter.Should().Be("expired-token");
        innerHandler.Requests[1].Headers.Authorization!.Parameter.Should().Be("fresh-token");
    }

    private static EsiAccessToken CreateToken(string accessToken) =>
        new(
            accessToken,
            "refresh-token",
            DateTimeOffset.UtcNow.AddMinutes(20),
            ["esi-skills.read_skills"],
            Maybe<CharacterId>.Some(new CharacterId(42)));

    private sealed class StubTokenProvider(
        Result<EsiAccessToken> accessTokenResult,
        Result<EsiAccessToken> refreshTokenResult) : IEsiTokenProvider
    {
        public int RefreshCallCount { get; private set; }

        public Task<Result<EsiAccessToken>> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(accessTokenResult);

        public Task<Result<EsiAccessToken>> RefreshAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            RefreshCallCount++;
            return Task.FromResult(refreshTokenResult);
        }
    }
}