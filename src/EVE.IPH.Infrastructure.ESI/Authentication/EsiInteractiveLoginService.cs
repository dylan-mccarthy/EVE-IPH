using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Interfaces;

namespace EVE.IPH.Infrastructure.ESI.Authentication;

/// <summary>
/// Orchestrates the browser-driven PKCE login flow from authorization request to token exchange.
/// </summary>
public sealed class EsiInteractiveLoginService(
    IEsiSsoClient ssoClient,
    IEsiCallbackListener callbackListener,
    IEsiBrowserLauncher browserLauncher) : IEsiInteractiveLoginService
{
    private readonly IEsiSsoClient _ssoClient = ssoClient ?? throw new ArgumentNullException(nameof(ssoClient));
    private readonly IEsiCallbackListener _callbackListener = callbackListener ?? throw new ArgumentNullException(nameof(callbackListener));
    private readonly IEsiBrowserLauncher _browserLauncher = browserLauncher ?? throw new ArgumentNullException(nameof(browserLauncher));

    public async Task<Result<EsiAccessToken>> AuthenticateAsync(
        IEnumerable<string> scopes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scopes);

        EsiAuthorizationRequest request = _ssoClient.CreateAuthorizationRequest(scopes);

        try
        {
            await _browserLauncher.OpenAsync(request.AuthorizationUri, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            return Result<EsiAccessToken>.Failure("ESI_BROWSER_OPEN_FAILED", ex.Message);
        }

        Result<EsiAuthorizationCallback> callbackResult = await _callbackListener.WaitForCallbackAsync(cancellationToken).ConfigureAwait(false);
        if (callbackResult.IsFailure)
        {
            return Result<EsiAccessToken>.Failure(callbackResult.Error);
        }

        EsiAuthorizationCallback callback = callbackResult.Value;
        if (callback.IsError)
        {
            return Result<EsiAccessToken>.Failure(
                "ESI_CALLBACK_ERROR",
                string.IsNullOrWhiteSpace(callback.ErrorDescription) ? callback.Error! : callback.ErrorDescription!);
        }

        if (!string.Equals(callback.State, request.State, StringComparison.Ordinal))
        {
            return Result<EsiAccessToken>.Failure("ESI_CALLBACK_STATE_MISMATCH", "The ESI callback state value did not match the login request.");
        }

        return await _ssoClient.ExchangeAuthorizationCodeAsync(callback.AuthorizationCode!, request.CodeVerifier, cancellationToken).ConfigureAwait(false);
    }
}