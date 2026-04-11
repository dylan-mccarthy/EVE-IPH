using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Assets.Services;
using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Assets.Tests.Services;

public sealed class AssetViewFilterServiceTests
{
    [Fact]
    public void Filter_FiltersByOwnerTypeAndBlueprintCopyAndSortsByQuantity()
    {
        AssetViewFilterService service = new();
        HydratedAsset[] assets =
        [
            CreateAsset(90000001, 1001, new TypeId(3001), 10, "Alpha Module", "Module", "Item", AssetBlueprintKind.Original),
            CreateAsset(90000001, 1002, new TypeId(3002), 3, "Beta Blueprint", "Blueprint", "Blueprint", AssetBlueprintKind.Copy),
            CreateAsset(90000002, 1003, new TypeId(3002), 25, "Gamma Blueprint", "Blueprint", "Blueprint", AssetBlueprintKind.Original),
        ];
        AssetViewRequest request = new(
            new HashSet<long> { 90000001 },
            new HashSet<long> { 3001, 3002 },
            string.Empty,
            true,
            AssetSortMode.Quantity);

        IReadOnlyList<HydratedAsset> result = service.Filter(assets, request);

        result.Select(asset => asset.ItemId).Should().Equal(1001, 1002);
    }

    [Fact]
    public void Filter_SearchesAcrossTypeAndLocationAndSortsByName()
    {
        AssetViewFilterService service = new();
        HydratedAsset[] assets =
        [
            CreateAsset(90000001, 1001, new TypeId(3001), 10, "Zeta Module", "Module", "Item", AssetBlueprintKind.Original, "Perimeter"),
            CreateAsset(90000001, 1002, new TypeId(3002), 3, "Alpha Rig", "Rig", "Item", AssetBlueprintKind.Original, "Jita"),
            CreateAsset(90000001, 1003, new TypeId(3003), 2, "Omega Charge", "Charge", "Item", AssetBlueprintKind.Original, "Jita"),
        ];
        AssetViewRequest request = new(
            new HashSet<long>(),
            new HashSet<long>(),
            "ji",
            false,
            AssetSortMode.Name);

        IReadOnlyList<HydratedAsset> result = service.Filter(assets, request);

        result.Select(asset => asset.TypeName).Should().Equal("Alpha Rig", "Omega Charge");
    }

    private static HydratedAsset CreateAsset(
        long ownerId,
        long itemId,
        TypeId typeId,
        long quantity,
        string typeName,
        string typeGroup,
        string typeCategory,
        AssetBlueprintKind blueprintKind,
        string locationName = "Jita")
    {
        return new HydratedAsset(
            ownerId,
            itemId,
            2001,
            typeId,
            quantity,
            0,
            quantity == 1,
            blueprintKind,
            string.Empty,
            typeName,
            typeGroup,
            typeCategory,
            locationName,
            string.Empty,
            false,
            0);
    }
}