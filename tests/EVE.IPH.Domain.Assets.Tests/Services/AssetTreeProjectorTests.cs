using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Assets.Services;

namespace EVE.IPH.Domain.Assets.Tests.Services;

public sealed class AssetTreeProjectorTests
{
    private readonly AssetTreeProjector _projector = new();

    [Fact]
    public void Project_BaseLocationAsset_AddsBaseNodeAndItemNode()
    {
        AssetTreeItem asset = CreateAsset(
            itemId: 100,
            locationId: 30000142,
            flagId: -5,
            flagText: "Space",
            container: false,
            itemText: "Tritanium");

        IReadOnlyList<AssetTreeNode> result = _projector.Project([asset]);

        result.Should().ContainSingle(node => node.Kind == AssetTreeNodeKind.BaseLocation && node.NodeId == "30000142");
        result.Should().ContainSingle(node => node.Kind == AssetTreeNodeKind.Item && node.NodeId == "100" && node.ParentNodeId == "30000142");
    }

    [Fact]
    public void Project_ContainerAssetUnderBaseLocation_UsesNegativeLocationIdForContainerNode()
    {
        AssetTreeItem asset = CreateAsset(
            itemId: 100,
            locationId: 60003760,
            flagId: -4,
            flagText: "Item Hangar",
            container: true,
            itemText: "Freighter");

        IReadOnlyList<AssetTreeNode> result = _projector.Project([asset]);

        result.Should().ContainSingle(node => node.Kind == AssetTreeNodeKind.Container && node.NodeId == "-60003760" && node.ParentNodeId == "60003760");
        result.Should().ContainSingle(node => node.Kind == AssetTreeNodeKind.Item && node.ParentNodeId == "-60003760");
    }

    [Fact]
    public void Project_ContainerAssetWithoutBaseLocation_UsesNegativeItemIdForContainerNode()
    {
        AssetTreeItem asset = CreateAsset(
            itemId: 100,
            locationId: 60003760,
            flagId: 5,
            flagText: "Cargo Hold",
            container: true,
            itemText: "Freighter");

        IReadOnlyList<AssetTreeNode> result = _projector.Project([asset]);

        result.Should().ContainSingle(node => node.Kind == AssetTreeNodeKind.Container && node.NodeId == "-100");
        result.Should().ContainSingle(node => node.Kind == AssetTreeNodeKind.Item && node.ParentNodeId == "-100");
    }

    [Fact]
    public void Project_ReusesExistingContainerNodeForSameLocationAndFlagText()
    {
        AssetTreeItem firstAsset = CreateAsset(
            itemId: 100,
            locationId: 60003760,
            flagId: -4,
            flagText: "Corp Hangar 1",
            container: true,
            itemText: "Ship A");
        AssetTreeItem secondAsset = CreateAsset(
            itemId: 101,
            locationId: 60003760,
            flagId: -4,
            flagText: "Corp Hangar 1",
            container: true,
            itemText: "Ship B");

        IReadOnlyList<AssetTreeNode> result = _projector.Project([firstAsset, secondAsset]);

        result.Count(node => node.Kind == AssetTreeNodeKind.Container).Should().Be(1);
        result.Count(node => node.Kind == AssetTreeNodeKind.Item).Should().Be(2);
        result.Where(node => node.Kind == AssetTreeNodeKind.Item).Select(node => node.ParentNodeId).Distinct().Should().ContainSingle().Which.Should().Be("-60003760");
    }

    [Fact]
    public void Project_UnknownStructure_DoesNotCreateContainerNode()
    {
        AssetTreeItem asset = CreateAsset(
            itemId: 100,
            locationId: 60003760,
            flagId: 4,
            flagText: "Item Hangar",
            container: true,
            locationName: "Unknown Structure",
            itemText: "Ship");

        IReadOnlyList<AssetTreeNode> result = _projector.Project([asset]);

        result.Should().NotContain(node => node.Kind == AssetTreeNodeKind.Container);
        result.Should().ContainSingle(node => node.Kind == AssetTreeNodeKind.Item && node.ParentNodeId == "60003760");
    }

    [Fact]
    public void Project_CelestialContainerItem_MarksItemAsContainerForSorting()
    {
        AssetTreeItem asset = CreateAsset(
            itemId: 100,
            locationId: 60003760,
            flagId: 4,
            flagText: "Hangar",
            container: false,
            typeGroup: "Freight Container",
            typeCategory: "Celestial",
            itemText: "Container");

        IReadOnlyList<AssetTreeNode> result = _projector.Project([asset]);

        result.Should().ContainSingle(node => node.Kind == AssetTreeNodeKind.Item && node.Container);
    }

    private static AssetTreeItem CreateAsset(
        long itemId,
        long locationId,
        int flagId,
        string flagText,
        bool container,
        string itemText,
        string locationName = "Jita IV - Moon 4",
        string typeGroup = "Ship",
        string typeCategory = "Material")
    {
        return new AssetTreeItem(
            itemId,
            locationId,
            locationName,
            flagId,
            10,
            flagText,
            container,
            false,
            50,
            typeGroup,
            typeCategory,
            itemText);
    }
}