using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Repositories.Sde;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteItemRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly IItemRepository _sut;

    public SqliteItemRepositoryTests()
    {
        _sut = new SqliteItemRepository(_fixture.ConnectionFactory);
        SeedData();
    }

    public void Dispose() => _fixture.Dispose();

    private void SeedData()
    {
        using System.Data.IDbConnection connection = _fixture.ConnectionFactory.CreateConnection();
        connection.Execute("INSERT OR IGNORE INTO INVENTORY_CATEGORIES (groupID, groupName, categoryID) VALUES (18, 'Mineral', 4)");
        connection.Execute("INSERT OR IGNORE INTO INVENTORY_TYPES (typeID, typeName, groupID, volume, portionSize) VALUES (34, 'Tritanium', 18, 0.01, 1)");
        connection.Execute("INSERT OR IGNORE INTO INVENTORY_TYPES (typeID, typeName, groupID, volume, portionSize) VALUES (35, 'Pyerite', 18, 0.01, 1)");
    }

    [Fact]
    public async Task GetItemAsync_ExistingType_ReturnsItemRecord()
    {
        Maybe<ItemRecord> result = await _sut.GetItemAsync(new TypeId(34));

        result.HasValue.Should().BeTrue();
        result.Value.TypeName.Should().Be("Tritanium");
        result.Value.GroupId.Should().Be(18);
        result.Value.GroupName.Should().Be("Mineral");
        result.Value.CategoryId.Should().Be(4);
    }

    [Fact]
    public async Task GetItemAsync_MissingType_ReturnsNone()
    {
        Maybe<ItemRecord> result = await _sut.GetItemAsync(new TypeId(999_999));

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task GetItemNameAsync_ExistingType_ReturnsName()
    {
        Maybe<string> result = await _sut.GetItemNameAsync(new TypeId(34));

        result.HasValue.Should().BeTrue();
        result.Value.Should().Be("Tritanium");
    }

    [Fact]
    public async Task GetItemNameAsync_MissingType_ReturnsNone()
    {
        Maybe<string> result = await _sut.GetItemNameAsync(new TypeId(888_888));

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task GetItemsByGroupAsync_ReturnsAllItemsInGroup()
    {
        Result<IReadOnlyList<ItemRecord>> result = await _sut.GetItemsByGroupAsync(18);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(i => i.TypeId.Value == 34);
        result.Value.Should().Contain(i => i.TypeId.Value == 35);
    }

    [Fact]
    public async Task GetItemsByGroupAsync_EmptyGroup_ReturnsEmptyList()
    {
        Result<IReadOnlyList<ItemRecord>> result = await _sut.GetItemsByGroupAsync(999_999);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
