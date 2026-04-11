using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Assets.Services;

public interface ICharacterAssetService
{
    Task<Result<IReadOnlyList<AssetRecord>>> GetAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AssetRecord>>> RefreshAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}