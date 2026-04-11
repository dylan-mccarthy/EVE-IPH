using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Assets.Services;

public interface ICorporationAssetService
{
    Task<Result<IReadOnlyList<AssetRecord>>> GetAsync(
        CorporationId corporationId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AssetRecord>>> RefreshAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        CancellationToken cancellationToken = default);
}