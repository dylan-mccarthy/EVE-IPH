using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Core.Tests.Identifiers;

public sealed class IdentifierTests
{
    [Fact]
    public void TypeId_WithSameValue_AreEqual()
    {
        TypeId a = new(12345L);
        TypeId b = new(12345L);

        a.Should().Be(b);
    }

    [Fact]
    public void TypeId_WithDifferentValues_AreNotEqual()
    {
        TypeId a = new(1L);
        TypeId b = new(2L);

        a.Should().NotBe(b);
    }

    [Fact]
    public void TypeId_ToString_ReturnsUnderlyingValue()
    {
        TypeId id = new(99L);

        id.ToString().Should().Be("99");
    }

    [Fact]
    public void BlueprintId_WithSameValue_AreEqual()
    {
        BlueprintId a = new(700L);
        BlueprintId b = new(700L);

        a.Should().Be(b);
    }

    [Fact]
    public void RegionId_WithSameValue_AreEqual()
    {
        RegionId a = new(10000002);
        RegionId b = new(10000002);

        a.Should().Be(b);
    }

    [Fact]
    public void SystemId_ToString_ReturnsUnderlyingValue()
    {
        SystemId id = new(30000142);

        id.ToString().Should().Be("30000142");
    }

    [Fact]
    public void CharacterId_WithSameValue_AreEqual()
    {
        CharacterId a = new(90000001L);
        CharacterId b = new(90000001L);

        a.Should().Be(b);
    }

    [Fact]
    public void CorporationId_WithDifferentValues_AreNotEqual()
    {
        CorporationId a = new(1L);
        CorporationId b = new(2L);

        a.Should().NotBe(b);
    }

    [Fact]
    public void AllianceId_ToString_ReturnsUnderlyingValue()
    {
        AllianceId id = new(500001L);

        id.ToString().Should().Be("500001");
    }

    [Fact]
    public void StationId_WithSameValue_AreEqual()
    {
        StationId a = new(60003760L);
        StationId b = new(60003760L);

        a.Should().Be(b);
    }

    [Fact]
    public void ItemId_WithDifferentValues_AreNotEqual()
    {
        ItemId a = new(100L);
        ItemId b = new(101L);

        a.Should().NotBe(b);
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(0L)]
    [InlineData(long.MaxValue)]
    public void TypeId_ToString_MatchesLongToString(long value)
    {
        TypeId id = new(value);

        id.ToString().Should().Be(value.ToString());
    }

    [Fact]
    public void TypeId_CanBeUsedAsDictionaryKey()
    {
        Dictionary<TypeId, string> dict = new()
        {
            [new TypeId(34L)] = "Tritanium",
            [new TypeId(35L)] = "Pyerite",
        };

        dict[new TypeId(34L)].Should().Be("Tritanium");
        dict[new TypeId(35L)].Should().Be("Pyerite");
    }
}
