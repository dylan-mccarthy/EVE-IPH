using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Authentication;
using EVE.IPH.Infrastructure.ESI.Interfaces;
using NSubstitute;

namespace EVE.IPH.Infrastructure.ESI.Tests.Authentication;

public sealed class EsiTokenProviderTests
{
    [Fact]
    public async Task GetAccessTokenAsync_WhenStoredTokenIsFresh_ReturnsStoredToken()
    {
        InMemoryTokenStore tokenStore = new(new EsiTokenRecord(
            "access-token",
            "refresh-token",
            DateTimeOffset.UtcNow.AddMinutes(10),
            ["esi-skills.read_skills"],
            Maybe<CharacterId>.Some(new CharacterId(1001))));

        IEsiSsoClient ssoClient = Substitute.For<IEsiSsoClient>();
        EsiTokenProvider provider = new(tokenStore, ssoClient, TimeProvider.System);

        var result = await provider.GetAccessTokenAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        await ssoClient.DidNotReceiveWithAnyArgs().RefreshAccessTokenAsync(default!, default);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenStoredTokenIsExpired_RefreshesToken()
    {
        InMemoryTokenStore tokenStore = new(new EsiTokenRecord(
            "expired-token",
            "refresh-token",
            DateTimeOffset.UtcNow.AddSeconds(10),
            ["esi-skills.read_skills"],
            Maybe<CharacterId>.Some(new CharacterId(1001))));

        IEsiSsoClient ssoClient = Substitute.For<IEsiSsoClient>();
        ssoClient.RefreshAccessTokenAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(Result<EsiAccessToken>.Success(new EsiAccessToken(
                "fresh-token",
                "refresh-token",
                DateTimeOffset.UtcNow.AddMinutes(20),
                ["esi-skills.read_skills"],
                Maybe<CharacterId>.Some(new CharacterId(1001)))));

        EsiTokenProvider provider = new(tokenStore, ssoClient, TimeProvider.System);

        var result = await provider.GetAccessTokenAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("fresh-token");
        await ssoClient.Received(1).RefreshAccessTokenAsync("refresh-token", Arg.Any<CancellationToken>());
    }

    private sealed class InMemoryTokenStore(EsiTokenRecord token) : IEsiTokenStore
    {
        private Maybe<EsiTokenRecord> _token = Maybe<EsiTokenRecord>.Some(token);

        public Task<Result<bool>> ClearAsync(CancellationToken cancellationToken = default)
        {
            _token = Maybe<EsiTokenRecord>.None;
            return Task.FromResult(Result<bool>.Success(true));
        }

        public Task<Maybe<EsiTokenRecord>> ReadAsync(CancellationToken cancellationToken = default) => Task.FromResult(_token);

        public Task<Result<EsiTokenRecord>> WriteAsync(EsiTokenRecord token, CancellationToken cancellationToken = default)
        {
            _token = Maybe<EsiTokenRecord>.Some(token);
            return Task.FromResult(Result<EsiTokenRecord>.Success(token));
        }
    }
}