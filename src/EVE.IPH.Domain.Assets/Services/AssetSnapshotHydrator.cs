using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Assets.Services;

public sealed class AssetSnapshotHydrator : IAssetSnapshotHydrator
{
    private const string UnknownItem = "Unknown Item";
    private const string UnknownGroup = "Unknown Group";
    private const string UnknownCategory = "Unknown Category";
    private const string UnknownLocation = "Unknown Location";

    public IReadOnlyList<HydratedAsset> Hydrate(
        IEnumerable<AssetRecord> assets,
        IReadOnlyDictionary<TypeId, AssetTypeMetadata> typeMetadata,
        IReadOnlyDictionary<long, AssetLocationMetadata> locationMetadata)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(typeMetadata);
        ArgumentNullException.ThrowIfNull(locationMetadata);

        return assets.Select(asset => HydrateAsset(asset, typeMetadata, locationMetadata)).ToArray();
    }

    private static HydratedAsset HydrateAsset(
        AssetRecord asset,
        IReadOnlyDictionary<TypeId, AssetTypeMetadata> typeMetadata,
        IReadOnlyDictionary<long, AssetLocationMetadata> locationMetadata)
    {
        ArgumentNullException.ThrowIfNull(asset);

        AssetTypeMetadata? metadata = typeMetadata.GetValueOrDefault(asset.TypeId);
        AssetLocationMetadata? location = locationMetadata.GetValueOrDefault(asset.LocationId);

        string baseTypeName = string.IsNullOrWhiteSpace(metadata?.TypeName) ? UnknownItem : metadata.TypeName;
        string displayTypeName = string.IsNullOrWhiteSpace(asset.ItemName)
            ? baseTypeName
            : asset.ItemName + " (" + baseTypeName + ")";

        return new HydratedAsset(
            asset.OwnerId,
            asset.ItemId,
            asset.LocationId,
            asset.TypeId,
            asset.Quantity,
            asset.FlagId,
            asset.IsSingleton,
            asset.IsBlueprintCopy ? AssetBlueprintKind.Copy : AssetBlueprintKind.Original,
            asset.ItemName,
            displayTypeName,
            string.IsNullOrWhiteSpace(metadata?.GroupName) ? UnknownGroup : metadata.GroupName,
            string.IsNullOrWhiteSpace(metadata?.CategoryName) ? UnknownCategory : metadata.CategoryName,
            string.IsNullOrWhiteSpace(location?.LocationName) ? UnknownLocation : location.LocationName,
            location?.FlagText ?? string.Empty,
            location?.Container ?? false,
            location?.FlagSort ?? 0);
    }
}