using System.Net;
using System.Net.Http.Headers;
using EVE.IPH.Infrastructure.ESI.Interfaces;

namespace EVE.IPH.Infrastructure.ESI.Authentication;

/// <summary>
/// Adds the current bearer token to outgoing ESI requests and retries once after a token refresh
/// when the upstream service responds with <see cref="HttpStatusCode.Unauthorized"/>.
/// </summary>
public sealed class BearerTokenHandler(IEsiTokenProvider tokenProvider) : DelegatingHandler
{
    private readonly IEsiTokenProvider _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        HttpResponseMessage response = await SendWithTokenAsync(
            request,
            () => _tokenProvider.GetAccessTokenAsync(cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();

        return await SendWithTokenAsync(
            request,
            () => _tokenProvider.RefreshAccessTokenAsync(cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendWithTokenAsync(
        HttpRequestMessage request,
        Func<Task<Domain.Core.Results.Result<EsiAccessToken>>> getToken,
        CancellationToken cancellationToken)
    {
        Domain.Core.Results.Result<EsiAccessToken> tokenResult = await getToken().ConfigureAwait(false);
        if (tokenResult.IsFailure)
        {
            throw new HttpRequestException(tokenResult.Error.ToString());
        }

        HttpRequestMessage authorizedRequest = await request.CloneAsync(cancellationToken).ConfigureAwait(false);
        authorizedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value.AccessToken);

        return await base.SendAsync(authorizedRequest, cancellationToken).ConfigureAwait(false);
    }
}