using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Infrastructure.ESI.Interfaces;

/// <summary>
/// Supplies the current ESI access token and performs refreshes when needed.
/// </summary>
public interface IEsiTokenProvider
{
    Task<Result<EsiAccessToken>> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    Task<Result<EsiAccessToken>> GetAccessTokenAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<EsiAccessToken>> RefreshAccessTokenAsync(CancellationToken cancellationToken = default);

    Task<Result<EsiAccessToken>> RefreshAccessTokenAsync(CharacterId characterId, CancellationToken cancellationToken = default);
}