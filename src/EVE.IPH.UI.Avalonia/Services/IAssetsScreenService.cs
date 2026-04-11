using EVE.IPH.Domain.Assets.Models;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IAssetsScreenService
{
    IReadOnlyList<HydratedAsset> GetHydratedAssets();
}