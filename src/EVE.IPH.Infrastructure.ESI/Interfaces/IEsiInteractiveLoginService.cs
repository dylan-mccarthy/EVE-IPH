using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Infrastructure.ESI.Interfaces;

/// <summary>
/// Coordinates the full interactive PKCE login flow.
/// </summary>
public interface IEsiInteractiveLoginService
{
    Task<Result<EsiAccessToken>> AuthenticateAsync(
        IEnumerable<string> scopes,
        CancellationToken cancellationToken = default);
}