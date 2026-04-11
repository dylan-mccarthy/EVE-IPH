using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Assets.Services;

namespace EVE.IPH.Domain.Assets.Tests.Services;

public sealed class AssetHierarchyServiceTests
{
    private readonly AssetHierarchyService _service = new();

    [Fact]
    public void FindAssetTree_ReturnsAssetAndRecursiveParents()
    {
        AssetHierarchyItem station = CreateAsset(1, 60003760, 100, "Station", AssetBlueprintKind.None);
        AssetHierarchyItem container = CreateAsset(2, 1, 101, "Structure", AssetBlueprintKind.None);
        AssetHierarchyItem child = CreateAsset(3, 2, 102, "Material", AssetBlueprintKind.None);

        IReadOnlyList<AssetHierarchyItem> result = _service.FindAssetTree(child, [station, container, child]);

        result.Should().BeEquivalentTo([station, container, child]);
    }

    [Fact]
    public void BuildMaterialAssetList_FiltersByTypeIdAndIncludesAncestorChain()
    {
        AssetHierarchyItem station = CreateAsset(1, 60003760, 100, "Station", AssetBlueprintKind.None);
        AssetHierarchyItem container = CreateAsset(2, 1, 101, "Structure", AssetBlueprintKind.None);
        AssetHierarchyItem tritanium = CreateAsset(3, 2, 34, "Material", AssetBlueprintKind.None);
        AssetHierarchyItem pyerite = CreateAsset(4, 2, 35, "Material", AssetBlueprintKind.None);

        IReadOnlyList<AssetHierarchyItem> result = _service.BuildMaterialAssetList(
            [station, container, tritanium, pyerite],
            [34],
            onlyBlueprintCopies: false);

        result.Should().BeEquivalentTo([station, container, tritanium]);
    }

    [Fact]
    public void BuildMaterialAssetList_OnlyBlueprintCopies_ExcludesOriginalBlueprints()
    {
        AssetHierarchyItem blueprintOriginal = CreateAsset(3, 2, 500, "Blueprint", AssetBlueprintKind.Original);
        AssetHierarchyItem blueprintCopy = CreateAsset(4, 2, 500, "Blueprint", AssetBlueprintKind.Copy);

        IReadOnlyList<AssetHierarchyItem> result = _service.BuildMaterialAssetList(
            [blueprintOriginal, blueprintCopy],
            [500],
            onlyBlueprintCopies: true);

        result.Should().ContainSingle().Which.Should().Be(blueprintCopy);
    }

    [Fact]
    public void BuildMaterialAssetList_NonBlueprintAssets_AreIncludedWhenOnlyBlueprintCopiesEnabled()
    {
        AssetHierarchyItem material = CreateAsset(3, 2, 34, "Material", AssetBlueprintKind.None);

        IReadOnlyList<AssetHierarchyItem> result = _service.BuildMaterialAssetList(
            [material],
            [34],
            onlyBlueprintCopies: true);

        result.Should().ContainSingle().Which.Should().Be(material);
    }

    private static AssetHierarchyItem CreateAsset(
        long itemId,
        long locationId,
        long typeId,
        string typeCategory,
        AssetBlueprintKind blueprintKind)
    {
        return new AssetHierarchyItem(itemId, locationId, typeId, typeCategory, blueprintKind);
    }
}