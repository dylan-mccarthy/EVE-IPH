using EVE.IPH.Domain.Assets.Models;

namespace EVE.IPH.Domain.Assets.Services;

public sealed class AssetViewFilterService : IAssetViewFilterService
{
    public IReadOnlyList<HydratedAsset> Filter(
        IEnumerable<HydratedAsset> assets,
        AssetViewRequest request)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(request);

        IEnumerable<HydratedAsset> filtered = assets;

        if (request.OwnerIds.Count > 0)
        {
            filtered = filtered.Where(asset => request.OwnerIds.Contains(asset.OwnerId));
        }

        if (request.TypeIds.Count > 0)
        {
            filtered = filtered.Where(asset => request.TypeIds.Contains(asset.TypeId.Value));
        }

        if (request.OnlyBlueprintCopies)
        {
            filtered = filtered.Where(asset =>
                !asset.TypeCategory.Equals("Blueprint", StringComparison.OrdinalIgnoreCase) ||
                asset.BlueprintKind == AssetBlueprintKind.Copy);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            filtered = filtered.Where(asset =>
                asset.TypeName.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase) ||
                asset.LocationName.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase));
        }

        return request.SortMode switch
        {
            AssetSortMode.Quantity => filtered
                .OrderByDescending(asset => asset.Quantity)
                .ThenBy(asset => asset.TypeName, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            _ => filtered
                .OrderBy(asset => asset.TypeName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(asset => asset.LocationName, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
        };
    }
}