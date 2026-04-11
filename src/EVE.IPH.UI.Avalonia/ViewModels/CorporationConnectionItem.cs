using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class CorporationConnectionItem(CorporationConnectionRecord corporation, string authorizedCharacterName)
{
    public CorporationConnectionRecord Corporation { get; } = corporation ?? throw new ArgumentNullException(nameof(corporation));

    public string AuthorizedCharacterName { get; } = authorizedCharacterName;

    public string Name => Corporation.Name;

    public CorporationId CorporationId => Corporation.CorporationId;

    public string AccessSummary => BuildAccessSummary(Corporation);

    private static string BuildAccessSummary(CorporationConnectionRecord corporation)
    {
        List<string> grantedAccess = [];

        if (corporation.HasAssetAccess)
        {
            grantedAccess.Add("Assets");
        }

        if (corporation.HasIndustryJobAccess)
        {
            grantedAccess.Add("Industry Jobs");
        }

        if (corporation.HasBlueprintAccess)
        {
            grantedAccess.Add("Blueprints");
        }

        return grantedAccess.Count == 0 ? "No scoped corporation access" : string.Join(", ", grantedAccess);
    }
}