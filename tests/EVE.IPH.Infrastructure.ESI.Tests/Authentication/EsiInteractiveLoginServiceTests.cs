using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Authentication;
using EVE.IPH.Infrastructure.ESI.Interfaces;
using NSubstitute;

namespace EVE.IPH.Infrastructure.ESI.Tests.Authentication;

public sealed class EsiInteractiveLoginServiceTests
{
    [Fact]
    public async Task AuthenticateAsync_WhenCallbackStateMatches_ExchangesAuthorizationCode()
    {
        IEsiSsoClient ssoClient = Substitute.For<IEsiSsoClient>();
        IEsiCallbackListener callbackListener = Substitute.For<IEsiCallbackListener>();
        IEsiBrowserLauncher browserLauncher = Substitute.For<IEsiBrowserLauncher>();

        EsiAuthorizationRequest authorizationRequest = new(
            new Uri("https://login.eveonline.com/v2/oauth/authorize?state=abc"),
            "expected-state",
            "verifier",
            "challenge",
            ["esi-skills.read_skills"]);

        ssoClient.CreateAuthorizationRequest(Arg.Any<IEnumerable<string>>()).Returns(authorizationRequest);
        callbackListener.WaitForCallbackAsync(Arg.Any<CancellationToken>())
            .Returns(Result<EsiAuthorizationCallback>.Success(new EsiAuthorizationCallback("auth-code", "expected-state", null, null)));

        EsiAccessToken token = new(
            "access-token",
            "refresh-token",
            DateTimeOffset.UtcNow.AddMinutes(30),
            ["esi-skills.read_skills"],
            Maybe<CharacterId>.Some(new CharacterId(42)));

        ssoClient.ExchangeAuthorizationCodeAsync("auth-code", "verifier", Arg.Any<CancellationToken>())
            .Returns(Result<EsiAccessToken>.Success(token));

        EsiInteractiveLoginService service = new(ssoClient, callbackListener, browserLauncher);

        Result<EsiAccessToken> result = await service.AuthenticateAsync(["esi-skills.read_skills"]);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        await browserLauncher.Received(1).OpenAsync(authorizationRequest.AuthorizationUri, Arg.Any<CancellationToken>());
        await ssoClient.Received(1).ExchangeAuthorizationCodeAsync("auth-code", "verifier", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthenticateAsync_WhenStateDoesNotMatch_ReturnsFailure()
    {
        IEsiSsoClient ssoClient = Substitute.For<IEsiSsoClient>();
        IEsiCallbackListener callbackListener = Substitute.For<IEsiCallbackListener>();
        IEsiBrowserLauncher browserLauncher = Substitute.For<IEsiBrowserLauncher>();

        ssoClient.CreateAuthorizationRequest(Arg.Any<IEnumerable<string>>())
            .Returns(new EsiAuthorizationRequest(new Uri("https://login.eveonline.com"), "expected-state", "verifier", "challenge", []));

        callbackListener.WaitForCallbackAsync(Arg.Any<CancellationToken>())
            .Returns(Result<EsiAuthorizationCallback>.Success(new EsiAuthorizationCallback("auth-code", "wrong-state", null, null)));

        EsiInteractiveLoginService service = new(ssoClient, callbackListener, browserLauncher);

        Result<EsiAccessToken> result = await service.AuthenticateAsync([]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ESI_CALLBACK_STATE_MISMATCH");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenCallbackReturnsError_ReturnsFailure()
    {
        IEsiSsoClient ssoClient = Substitute.For<IEsiSsoClient>();
        IEsiCallbackListener callbackListener = Substitute.For<IEsiCallbackListener>();
        IEsiBrowserLauncher browserLauncher = Substitute.For<IEsiBrowserLauncher>();

        ssoClient.CreateAuthorizationRequest(Arg.Any<IEnumerable<string>>())
            .Returns(new EsiAuthorizationRequest(new Uri("https://login.eveonline.com"), "expected-state", "verifier", "challenge", []));

        callbackListener.WaitForCallbackAsync(Arg.Any<CancellationToken>())
            .Returns(Result<EsiAuthorizationCallback>.Success(new EsiAuthorizationCallback(null, "expected-state", "access_denied", "The login was cancelled.")));

        EsiInteractiveLoginService service = new(ssoClient, callbackListener, browserLauncher);

        Result<EsiAccessToken> result = await service.AuthenticateAsync([]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ESI_CALLBACK_ERROR");
        result.Error.Message.Should().Be("The login was cancelled.");
    }
}