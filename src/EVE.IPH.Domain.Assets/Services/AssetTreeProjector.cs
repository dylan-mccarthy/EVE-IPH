using EVE.IPH.Domain.Assets.Models;

namespace EVE.IPH.Domain.Assets.Services;

public sealed class AssetTreeProjector : IAssetTreeProjector
{
    public IReadOnlyList<AssetTreeNode> Project(IEnumerable<AssetTreeItem> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);

        List<AssetTreeNode> nodes = [];

        foreach (AssetTreeItem asset in assets)
        {
            ArgumentNullException.ThrowIfNull(asset);

            bool baseNodeAdded = false;
            long containerLocationId = 0;
            bool inContainer = false;

            if (asset.FlagId <= 0)
            {
                AssetTreeNode baseNode = new(
                    asset.LocationId.ToString(),
                    null,
                    asset.LocationName,
                    AssetTreeNodeKind.BaseLocation,
                    asset.FlagId,
                    -1,
                    true,
                    true,
                    0);

                AddNode(nodes, baseNode);
                baseNodeAdded = true;
            }

            if (asset.Container && !asset.LocationName.Equals("Unknown Structure", StringComparison.Ordinal))
            {
                containerLocationId = baseNodeAdded ? -asset.LocationId : -asset.ItemId;

                AssetTreeNode containerNode = new(
                    containerLocationId.ToString(),
                    asset.LocationId.ToString(),
                    asset.FlagText,
                    AssetTreeNodeKind.Container,
                    asset.FlagId,
                    asset.FlagSort,
                    true,
                    true,
                    0);

                AssetTreeNode? existingContainer = nodes.FirstOrDefault(node =>
                    node.Kind == AssetTreeNodeKind.Container &&
                    node.DisplayText == containerNode.DisplayText &&
                    node.ParentNodeId == containerNode.ParentNodeId);

                if (existingContainer is not null)
                {
                    containerLocationId = long.Parse(existingContainer.NodeId);
                }
                else
                {
                    AddNode(nodes, containerNode);
                }

                inContainer = true;
            }

            AssetTreeNode itemNode = new(
                asset.ItemId.ToString(),
                inContainer ? containerLocationId.ToString() : asset.LocationId.ToString(),
                asset.ItemText,
                AssetTreeNodeKind.Item,
                asset.FlagId,
                asset.FlagSort,
                asset.TypeGroup.Contains("Container", StringComparison.Ordinal) &&
                asset.TypeCategory.Equals("Celestial", StringComparison.Ordinal),
                asset.IsSingleton,
                asset.IsSingleton ? 0 : asset.Quantity);

            AddNode(nodes, itemNode);
        }

        return nodes;
    }

    private static void AddNode(List<AssetTreeNode> nodes, AssetTreeNode node)
    {
        if (!nodes.Any(existing => existing.NodeId == node.NodeId))
        {
            nodes.Add(node);
        }
    }
}