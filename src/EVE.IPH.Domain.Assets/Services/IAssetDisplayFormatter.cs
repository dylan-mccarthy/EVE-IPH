using EVE.IPH.Domain.Assets.Models;

namespace EVE.IPH.Domain.Assets.Services;

public interface IAssetDisplayFormatter
{
    string FormatLocationName(string locationName, string flagText);

    string FormatItemText(AssetDisplayItem item, bool isParentNode, string? industryActivityName = null);
}