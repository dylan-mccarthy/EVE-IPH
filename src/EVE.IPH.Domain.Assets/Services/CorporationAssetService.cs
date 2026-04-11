using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Assets.Services;

public sealed class CorporationAssetService(
    IAssetRepository assetRepository,
    IAssetDataSource assetDataSource) : ICorporationAssetService
{
    private readonly IAssetRepository _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
    private readonly IAssetDataSource _assetDataSource = assetDataSource ?? throw new ArgumentNullException(nameof(assetDataSource));

    public async Task<Result<IReadOnlyList<AssetRecord>>> GetAsync(
        CorporationId corporationId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<StoredAssetRecord>> storedAssets = await _assetRepository
            .GetByOwnerIdAsync(corporationId.Value, cancellationToken)
            .ConfigureAwait(false);

        return storedAssets.IsSuccess
            ? Result<IReadOnlyList<AssetRecord>>.Success(storedAssets.Value.Select(MapRecord).ToArray())
            : Result<IReadOnlyList<AssetRecord>>.Failure(storedAssets.Error);
    }

    public async Task<Result<IReadOnlyList<AssetRecord>>> RefreshAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<AssetData>> currentAssets = await _assetDataSource
            .GetCorporationAssetsAsync(corporationId, authenticatedCharacterId, cancellationToken)
            .ConfigureAwait(false);

        if (currentAssets.IsFailure)
        {
            return Result<IReadOnlyList<AssetRecord>>.Failure(currentAssets.Error);
        }

        IReadOnlyList<StoredAssetRecord> normalizedAssets = currentAssets.Value
            .Select(asset => new StoredAssetRecord(
                asset.OwnerId,
                asset.ItemId,
                asset.LocationId,
                asset.TypeId,
                asset.Quantity,
                asset.FlagId,
                asset.IsSingleton,
                asset.IsBlueprintCopy,
                asset.ItemName))
            .ToArray();

        Result<IReadOnlyList<StoredAssetRecord>> storedAssets = await _assetRepository
            .ReplaceAsync(corporationId.Value, normalizedAssets, cancellationToken)
            .ConfigureAwait(false);

        return storedAssets.IsSuccess
            ? Result<IReadOnlyList<AssetRecord>>.Success(storedAssets.Value.Select(MapRecord).ToArray())
            : Result<IReadOnlyList<AssetRecord>>.Failure(storedAssets.Error);
    }

    private static AssetRecord MapRecord(StoredAssetRecord asset) => new(
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