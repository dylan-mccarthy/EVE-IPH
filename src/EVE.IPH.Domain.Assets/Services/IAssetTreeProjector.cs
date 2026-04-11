using EVE.IPH.Domain.Assets.Models;

namespace EVE.IPH.Domain.Assets.Services;

public interface IAssetTreeProjector
{
    IReadOnlyList<AssetTreeNode> Project(IEnumerable<AssetTreeItem> assets);
}