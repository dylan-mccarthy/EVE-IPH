using EVE.IPH.Domain.Assets.Models;

namespace EVE.IPH.Domain.Assets.Services;

public interface IAssetViewFilterService
{
    IReadOnlyList<HydratedAsset> Filter(
        IEnumerable<HydratedAsset> assets,
        AssetViewRequest request);
}