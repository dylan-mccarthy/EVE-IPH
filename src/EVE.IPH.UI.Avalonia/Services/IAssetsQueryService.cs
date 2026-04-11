using EVE.IPH.Domain.Assets.Models;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IAssetsQueryService
{
    Task<AssetsScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default);
}

public sealed record AssetsScreenData(
    IReadOnlyList<HydratedAsset> Assets,
    IReadOnlyList<AssetOwnerFilterOption> OwnerOptions,
    string StatusText);

public sealed record AssetOwnerFilterOption(long? OwnerId, string DisplayName);