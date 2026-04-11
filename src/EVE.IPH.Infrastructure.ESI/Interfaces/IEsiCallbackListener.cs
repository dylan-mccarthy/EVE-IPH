using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Authentication;

namespace EVE.IPH.Infrastructure.ESI.Interfaces;

/// <summary>
/// Listens for the local OAuth redirect callback and extracts the returned authorization data.
/// </summary>
public interface IEsiCallbackListener
{
    Task<Result<EsiAuthorizationCallback>> WaitForCallbackAsync(CancellationToken cancellationToken = default);
}