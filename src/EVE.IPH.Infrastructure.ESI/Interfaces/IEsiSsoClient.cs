using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Authentication;

namespace EVE.IPH.Infrastructure.ESI.Interfaces;

/// <summary>
/// Handles the EVE SSO login and token exchange flow.
/// </summary>
public interface IEsiSsoClient
{
    EsiAuthorizationRequest CreateAuthorizationRequest(IEnumerable<string> scopes);

    Task<Result<EsiAccessToken>> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string codeVerifier,
        CancellationToken cancellationToken = default);

    Task<Result<EsiAccessToken>> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}