using EVE.IPH.Domain.Assets.Models;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IAssetsScreenService
{
    Task<AssetsScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default);

    Task<AssetsScreenData> RefreshAsync(CancellationToken cancellationToken = default);
}

public sealed record AssetsScreenData(
    IReadOnlyList<HydratedAsset> Assets,
    IReadOnlyList<AssetOwnerFilterOption> OwnerOptions,
    string StatusText);

public sealed record AssetOwnerFilterOption(long? OwnerId, string DisplayName);