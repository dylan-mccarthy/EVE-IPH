using EVE.IPH.Domain.Assets.Models;

namespace EVE.IPH.Domain.Assets.Services;

public interface IAssetHierarchyService
{
    IReadOnlyList<AssetHierarchyItem> BuildMaterialAssetList(
        IReadOnlyList<AssetHierarchyItem> assets,
        IReadOnlyList<long> typeIds,
        bool onlyBlueprintCopies);

    IReadOnlyList<AssetHierarchyItem> FindAssetTree(
        AssetHierarchyItem searchAsset,
        IReadOnlyList<AssetHierarchyItem> assets);
}