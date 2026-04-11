using EVE.IPH.Infrastructure.ESI.Authentication;

namespace EVE.IPH.Infrastructure.ESI.Tests.Authentication;

public sealed class TcpEsiCallbackListenerTests
{
    [Fact]
    public void ParseRequestLine_WithAuthorizationCode_ExtractsCodeAndState()
    {
        var result = TcpEsiCallbackListener.ParseRequestLine("GET /?code=abc123&state=state-1 HTTP/1.1");

        result.IsSuccess.Should().BeTrue();
        result.Value.AuthorizationCode.Should().Be("abc123");
        result.Value.State.Should().Be("state-1");
        result.Value.IsError.Should().BeFalse();
    }

    [Fact]
    public void ParseRequestLine_WithOAuthError_ExtractsErrorFields()
    {
        var result = TcpEsiCallbackListener.ParseRequestLine("GET /?error=access_denied&error_description=Denied&state=state-1 HTTP/1.1");

        result.IsSuccess.Should().BeTrue();
        result.Value.IsError.Should().BeTrue();
        result.Value.Error.Should().Be("access_denied");
        result.Value.ErrorDescription.Should().Be("Denied");
    }

    [Fact]
    public void ParseRequestLine_WithoutCode_ReturnsFailure()
    {
        var result = TcpEsiCallbackListener.ParseRequestLine("GET /?state=state-1 HTTP/1.1");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ESI_CALLBACK_CODE_MISSING");
    }
}