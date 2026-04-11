using System.Globalization;
using EVE.IPH.Domain.Assets.Models;

namespace EVE.IPH.Domain.Assets.Services;

public sealed class AssetDisplayFormatter : IAssetDisplayFormatter
{
    private const int IndustryJobFlag = 506;
    private const string QuantitySpacer = " - ";

    public string FormatLocationName(string locationName, string flagText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(flagText);

        if (flagText.Equals("Space", StringComparison.OrdinalIgnoreCase) ||
            flagText.Equals("Ship Offline", StringComparison.OrdinalIgnoreCase))
        {
            return locationName + " (In Solar System)";
        }

        return locationName;
    }

    public string FormatItemText(AssetDisplayItem item, bool isParentNode, string? industryActivityName = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        string itemName = item.TypeCategory.Equals("Blueprint", StringComparison.OrdinalIgnoreCase)
            ? item.BlueprintKind switch
            {
                AssetBlueprintKind.Original => item.TypeName + " (Original)",
                AssetBlueprintKind.Copy => item.TypeName + " (Copy)",
                _ => item.TypeName,
            }
            : item.TypeName;

        if (Math.Abs(item.FlagId) == IndustryJobFlag)
        {
            itemName += string.IsNullOrWhiteSpace(industryActivityName)
                ? " - Industry Job"
                : $" - {industryActivityName} Job";
        }

        if (!isParentNode && !item.IsSingleton)
        {
            return itemName + QuantitySpacer + item.Quantity.ToString("N0", CultureInfo.InvariantCulture);
        }

        return itemName;
    }
}