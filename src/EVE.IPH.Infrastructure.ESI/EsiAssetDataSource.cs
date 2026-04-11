using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Interfaces;
using EVE.IPH.Infrastructure.ESI.Models;

namespace EVE.IPH.Infrastructure.ESI;

public sealed class EsiAssetDataSource(IEsiClient esiClient) : IAssetDataSource
{
    private readonly IEsiClient _esiClient = esiClient ?? throw new ArgumentNullException(nameof(esiClient));

    public async Task<Result<IReadOnlyList<AssetData>>> GetCharacterAssetsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<EsiAsset>> assets = await _esiClient
            .GetCharacterAssetsAsync(characterId, cancellationToken)
            .ConfigureAwait(false);

        return assets.IsSuccess
            ? Result<IReadOnlyList<AssetData>>.Success(assets.Value.Select(MapAsset).ToArray())
            : Result<IReadOnlyList<AssetData>>.Failure(assets.Error);
    }

    public async Task<Result<IReadOnlyList<AssetData>>> GetCorporationAssetsAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<EsiAsset>> assets = await _esiClient
            .GetCorporationAssetsAsync(corporationId, authenticatedCharacterId, cancellationToken)
            .ConfigureAwait(false);

        return assets.IsSuccess
            ? Result<IReadOnlyList<AssetData>>.Success(assets.Value.Select(MapAsset).ToArray())
            : Result<IReadOnlyList<AssetData>>.Failure(assets.Error);
    }

    private static AssetData MapAsset(EsiAsset asset) => new(
        asset.OwnerId,
        asset.ItemId,
        asset.LocationId,
        asset.TypeId,
        asset.Quantity,
        asset.FlagId,
        asset.IsSingleton,
        asset.IsBlueprintCopy,
        asset.ItemName);
}