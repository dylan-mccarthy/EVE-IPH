namespace EVE.IPH.UI.Avalonia.Services;

public interface IAssetsCommandService
{
    Task<AssetsScreenData> RefreshAsync(CancellationToken cancellationToken = default);
}