using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Assets.Services;

public interface IAssetSnapshotHydrator
{
    IReadOnlyList<HydratedAsset> Hydrate(
        IEnumerable<AssetRecord> assets,
        IReadOnlyDictionary<TypeId, AssetTypeMetadata> typeMetadata,
        IReadOnlyDictionary<long, AssetLocationMetadata> locationMetadata);
}