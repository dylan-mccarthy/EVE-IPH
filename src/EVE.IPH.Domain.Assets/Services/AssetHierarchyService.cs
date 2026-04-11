using EVE.IPH.Domain.Assets.Models;

namespace EVE.IPH.Domain.Assets.Services;

public sealed class AssetHierarchyService : IAssetHierarchyService
{
    public IReadOnlyList<AssetHierarchyItem> BuildMaterialAssetList(
        IReadOnlyList<AssetHierarchyItem> assets,
        IReadOnlyList<long> typeIds,
        bool onlyBlueprintCopies)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(typeIds);

        List<AssetHierarchyItem> foundAssets = [];

        foreach (long typeId in typeIds)
        {
            foreach (AssetHierarchyItem asset in assets.Where(asset => asset.TypeId == typeId))
            {
                if (foundAssets.Contains(asset))
                {
                    continue;
                }

                bool includeAsset = asset.TypeCategory != "Blueprint"
                    || !onlyBlueprintCopies
                    || asset.BlueprintKind == AssetBlueprintKind.Copy;

                if (includeAsset)
                {
                    foundAssets.Add(asset);
                }
            }
        }

        List<AssetHierarchyItem> buildMaterialAssets = [];

        foreach (AssetHierarchyItem asset in foundAssets)
        {
            foreach (AssetHierarchyItem hierarchyAsset in FindAssetTree(asset, assets))
            {
                if (!buildMaterialAssets.Contains(hierarchyAsset))
                {
                    buildMaterialAssets.Add(hierarchyAsset);
                }
            }
        }

        return buildMaterialAssets;
    }

    public IReadOnlyList<AssetHierarchyItem> FindAssetTree(
        AssetHierarchyItem searchAsset,
        IReadOnlyList<AssetHierarchyItem> assets)
    {
        ArgumentNullException.ThrowIfNull(searchAsset);
        ArgumentNullException.ThrowIfNull(assets);

        List<AssetHierarchyItem> treeAssets = [];
        List<AssetHierarchyItem> parentAssets = assets
            .Where(asset => asset.ItemId == searchAsset.LocationId)
            .ToList();

        foreach (AssetHierarchyItem asset in parentAssets)
        {
            foreach (AssetHierarchyItem recursiveAsset in FindAssetTree(asset, assets))
            {
                if (!treeAssets.Contains(recursiveAsset))
                {
                    treeAssets.Add(recursiveAsset);
                }
            }
        }

        if (!treeAssets.Contains(searchAsset))
        {
            treeAssets.Add(searchAsset);
        }

        return treeAssets;
    }
}