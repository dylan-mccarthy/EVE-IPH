using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Industry.Models;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class Phase11SampleDataProvider : IPhase11SampleDataProvider
{
    public IReadOnlyList<AssetRecord> GetAssetRecords() =>
    [
        new AssetRecord(1001, 5000001, 6000001, new TypeId(101), 12, 11, true, false, string.Empty),
        new AssetRecord(1001, 5000002, 6000002, new TypeId(102), 3, 27, true, true, "Vargur Blueprint"),
        new AssetRecord(2001, 5000003, 6000003, new TypeId(103), 4200, 5, false, false, string.Empty),
    ];

    public IReadOnlyDictionary<TypeId, AssetTypeMetadata> GetAssetTypeMetadata() =>
        new Dictionary<TypeId, AssetTypeMetadata>
        {
            [new TypeId(101)] = new AssetTypeMetadata(new TypeId(101), "Fernite Carbide Composite Armor Plate", "Materials", "Material"),
            [new TypeId(102)] = new AssetTypeMetadata(new TypeId(102), "Vargur Blueprint", "Blueprints", "Blueprint"),
            [new TypeId(103)] = new AssetTypeMetadata(new TypeId(103), "Heavy Water", "Ice Products", "Material"),
        };

    public IReadOnlyDictionary<long, AssetLocationMetadata> GetAssetLocationMetadata() =>
        new Dictionary<long, AssetLocationMetadata>
        {
            [6000001] = new AssetLocationMetadata("Jita 4-4", "Item Hangar", false, 1),
            [6000002] = new AssetLocationMetadata("Tatara Alpha", "Blueprint Vault", true, 2),
            [6000003] = new AssetLocationMetadata("Perimeter Refinery", "Deliveries", false, 3),
        };

    public IReadOnlyList<IndustryJobViewItem> GetIndustryJobs()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        return
        [
            new IndustryJobViewItem(
                new IndustryJob(900001, 1001, 1, "active", now.AddHours(-6), now.AddHours(10)),
                "Kara Maken",
                "Manufacturing",
                "Vargur",
                "Vargur",
                "Ship",
                "Jita",
                "The Forge",
                1,
                2,
                0,
                "Tatara Alpha",
                "Ship Hangar",
                IndustryJobScope.Personal),
            new IndustryJobViewItem(
                new IndustryJob(900002, 2001, 9, "active", now.AddHours(-18), now.AddHours(-1)),
                "Oren Taz",
                "Reaction",
                "Composite Reaction",
                "Fernite Composite",
                "Reaction",
                "Perimeter",
                "The Forge",
                0,
                20,
                20,
                "Perimeter Refinery",
                "Reaction Hangar",
                IndustryJobScope.Corporation),
            new IndustryJobViewItem(
                new IndustryJob(900003, 1001, 4, "delivered", now.AddDays(-2), now.AddDays(-1)),
                "Kara Maken",
                "Research",
                "Vargur Blueprint",
                "Vargur Blueprint",
                "Blueprint",
                "Jita",
                "The Forge",
                10,
                10,
                10,
                "Tatara Alpha",
                "Blueprint Vault",
                IndustryJobScope.Personal),
        ];
    }

    public IReadOnlyList<ResearchAgent> GetResearchAgents() =>
    [
        new ResearchAgent(700001, new TypeId(204), "Arajna Yashar", "Mechanical Engineering", 1860, 115.5, 4, "Rens VIII - Moon 12", DateTimeOffset.UtcNow.AddDays(-32), 0),
        new ResearchAgent(700002, new TypeId(205), "Loras Tzash", "Gallente Starship Engineering", 980, 88.1, 4, "Dodixie IX - Moon 20", DateTimeOffset.UtcNow.AddDays(-14), 0),
        new ResearchAgent(700003, new TypeId(206), "Tsurion Malkalen", "Rocket Science", 1440, 102.4, 5, "Jita IV - Moon 4", DateTimeOffset.UtcNow.AddDays(-27), 0),
    ];

    public IReadOnlyDictionary<string, double> GetDatacorePrices() =>
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["Datacore - Mechanical Engineering"] = 165000,
            ["Datacore - Gallentean Starship Engineering"] = 312000,
            ["Datacore - Rocket Science"] = 141500,
        };

    public double GetDatacoreRedeemCost() => 10000;
}