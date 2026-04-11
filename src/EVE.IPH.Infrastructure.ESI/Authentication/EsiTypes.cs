namespace EVE.IPH.Infrastructure.ESI.Authentication;

/// <summary>
/// Authorization request details for browser-based PKCE login.
/// </summary>
public sealed record EsiAuthorizationRequest(
    Uri AuthorizationUri,
    string State,
    string CodeVerifier,
    string CodeChallenge,
    IReadOnlyList<string> Scopes);

/// <summary>
/// Data returned from the local OAuth callback.
/// </summary>
public sealed record EsiAuthorizationCallback(
    string? AuthorizationCode,
    string? State,
    string? Error,
    string? ErrorDescription)
{
    public bool IsError => !string.IsNullOrWhiteSpace(Error);

    public bool HasAuthorizationCode => !string.IsNullOrWhiteSpace(AuthorizationCode);
}

/// <summary>
/// Generated PKCE verifier and challenge pair.
/// </summary>
public sealed record EsiPkceChallenge(string Verifier, string Challenge);

/// <summary>
/// Static ESI SSO endpoints and application identity.
/// </summary>
public sealed record EsiSsoOptions(
    string ClientId,
    string RedirectUri,
    string AuthorizationEndpoint,
    string TokenEndpoint)
{
    public static EsiSsoOptions Default { get; } = new(
        "2737513b64854fa0a309e125419f8eff",
        "http://127.0.0.1:12500",
        "https://login.eveonline.com/v2/oauth/authorize",
        "https://login.eveonline.com/v2/oauth/token");
}