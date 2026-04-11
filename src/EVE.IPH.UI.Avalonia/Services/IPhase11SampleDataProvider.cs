using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Industry.Models;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IPhase11SampleDataProvider
{
    IReadOnlyList<AssetRecord> GetAssetRecords();

    IReadOnlyDictionary<TypeId, AssetTypeMetadata> GetAssetTypeMetadata();

    IReadOnlyDictionary<long, AssetLocationMetadata> GetAssetLocationMetadata();

    IReadOnlyList<IndustryJobViewItem> GetIndustryJobs();

    IReadOnlyList<ResearchAgent> GetResearchAgents();

    IReadOnlyDictionary<string, double> GetDatacorePrices();

    double GetDatacoreRedeemCost();
}