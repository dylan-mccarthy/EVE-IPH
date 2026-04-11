using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Assets.Services;
using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Assets.Tests.Services;

public sealed class AssetSnapshotHydratorTests
{
    [Fact]
    public void Hydrate_AppliesMetadataLocationAndItemNameOverlay()
    {
        AssetSnapshotHydrator service = new();
        AssetRecord asset = new(90000001, 1001, 2001, new TypeId(3001), 5, 4, false, true, "Can 1");
        Dictionary<TypeId, AssetTypeMetadata> typeMetadata = new()
        {
            [new TypeId(3001)] = new AssetTypeMetadata(new TypeId(3001), "Blueprint Alpha", "Blueprint", "Blueprint"),
        };
        Dictionary<long, AssetLocationMetadata> locationMetadata = new()
        {
            [2001] = new AssetLocationMetadata("Jita IV", "Space", true, 90),
        };

        IReadOnlyList<HydratedAsset> result = service.Hydrate([asset], typeMetadata, locationMetadata);

        result.Should().ContainSingle();
        result[0].TypeName.Should().Be("Can 1 (Blueprint Alpha)");
        result[0].TypeGroup.Should().Be("Blueprint");
        result[0].TypeCategory.Should().Be("Blueprint");
        result[0].LocationName.Should().Be("Jita IV");
        result[0].FlagText.Should().Be("Space");
        result[0].Container.Should().BeTrue();
        result[0].BlueprintKind.Should().Be(AssetBlueprintKind.Copy);
    }

    [Fact]
    public void Hydrate_UsesUnknownFallbacksWhenLookupMissing()
    {
        AssetSnapshotHydrator service = new();
        AssetRecord asset = new(90000001, 1001, 2001, new TypeId(3001), 1, 0, true, false, string.Empty);

        IReadOnlyList<HydratedAsset> result = service.Hydrate([asset], new Dictionary<TypeId, AssetTypeMetadata>(), new Dictionary<long, AssetLocationMetadata>());

        result.Should().ContainSingle();
        result[0].TypeName.Should().Be("Unknown Item");
        result[0].TypeGroup.Should().Be("Unknown Group");
        result[0].TypeCategory.Should().Be("Unknown Category");
        result[0].LocationName.Should().Be("Unknown Location");
        result[0].BlueprintKind.Should().Be(AssetBlueprintKind.Original);
    }
}