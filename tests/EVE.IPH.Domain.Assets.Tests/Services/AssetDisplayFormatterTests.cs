using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Assets.Services;

namespace EVE.IPH.Domain.Assets.Tests.Services;

public sealed class AssetDisplayFormatterTests
{
    private readonly AssetDisplayFormatter _formatter = new();

    [Fact]
    public void FormatLocationName_SpaceFlag_AppendsSolarSystemSuffix()
    {
        string result = _formatter.FormatLocationName("Jita IV - Moon 4", "Space");

        result.Should().Be("Jita IV - Moon 4 (In Solar System)");
    }

    [Fact]
    public void FormatLocationName_OtherFlag_ReturnsOriginalName()
    {
        string result = _formatter.FormatLocationName("Jita IV - Moon 4", "Hangar");

        result.Should().Be("Jita IV - Moon 4");
    }

    [Fact]
    public void FormatItemText_BlueprintCopy_AppendsCopyMarker()
    {
        AssetDisplayItem item = new(
            "Rifter Blueprint",
            "Blueprint",
            AssetBlueprintKind.Copy,
            1,
            5,
            true);

        string result = _formatter.FormatItemText(item, isParentNode: false);

        result.Should().Be("Rifter Blueprint (Copy)");
    }

    [Fact]
    public void FormatItemText_IndustryJobWithoutActivityName_UsesFallbackSuffix()
    {
        AssetDisplayItem item = new(
            "Rifter Blueprint",
            "Blueprint",
            AssetBlueprintKind.Original,
            1,
            -506,
            true);

        string result = _formatter.FormatItemText(item, isParentNode: false);

        result.Should().Be("Rifter Blueprint (Original) - Industry Job");
    }

    [Fact]
    public void FormatItemText_IndustryJobWithActivityName_UsesActivitySuffix()
    {
        AssetDisplayItem item = new(
            "Rifter Blueprint",
            "Blueprint",
            AssetBlueprintKind.Original,
            1,
            506,
            true);

        string result = _formatter.FormatItemText(item, isParentNode: false, industryActivityName: "Manufacturing");

        result.Should().Be("Rifter Blueprint (Original) - Manufacturing Job");
    }

    [Fact]
    public void FormatItemText_StackedChildItem_AppendsFormattedQuantity()
    {
        AssetDisplayItem item = new(
            "Tritanium",
            "Material",
            AssetBlueprintKind.None,
            12345,
            5,
            false);

        string result = _formatter.FormatItemText(item, isParentNode: false);

        result.Should().Be("Tritanium - 12,345");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void FormatItemText_ParentOrSingleton_DoesNotAppendQuantity(bool isParentNode, bool isSingleton)
    {
        AssetDisplayItem item = new(
            "Tritanium",
            "Material",
            AssetBlueprintKind.None,
            100,
            5,
            isSingleton);

        string result = _formatter.FormatItemText(item, isParentNode);

        result.Should().Be("Tritanium");
    }
}