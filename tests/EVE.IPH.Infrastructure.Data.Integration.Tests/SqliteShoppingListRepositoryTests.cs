using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteShoppingListRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly IShoppingListRepository _sut;

    public SqliteShoppingListRepositoryTests()
    {
        _sut = new SqliteShoppingListRepository(_fixture.ConnectionFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task UpsertItemAsync_NewItem_InsertsAndReturnsRecord()
    {
        ShoppingListItemRecord item = new(new TypeId(34), "Tritanium", 1000, 5.5);

        Result<ShoppingListItemRecord> result = await _sut.UpsertItemAsync(item);

        result.IsSuccess.Should().BeTrue();
        result.Value.ItemName.Should().Be("Tritanium");
        result.Value.Quantity.Should().Be(1000);
    }

    [Fact]
    public async Task GetItemsAsync_AfterInsert_ReturnsItems()
    {
        await _sut.UpsertItemAsync(new ShoppingListItemRecord(new TypeId(35), "Pyerite", 500, 7.0));
        await _sut.UpsertItemAsync(new ShoppingListItemRecord(new TypeId(36), "Mexallon", 200, 50.0));

        Result<IReadOnlyList<ShoppingListItemRecord>> result = await _sut.GetItemsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(i => i.TypeId.Value == 35);
        result.Value.Should().Contain(i => i.TypeId.Value == 36);
    }

    [Fact]
    public async Task UpsertItemAsync_ExistingItem_UpdatesQuantityAndPrice()
    {
        TypeId typeId = new(37);
        await _sut.UpsertItemAsync(new ShoppingListItemRecord(typeId, "Isogen", 100, 30.0));
        await _sut.UpsertItemAsync(new ShoppingListItemRecord(typeId, "Isogen", 500, 35.0));

        Result<IReadOnlyList<ShoppingListItemRecord>> result = await _sut.GetItemsAsync();

        result.IsSuccess.Should().BeTrue();
        ShoppingListItemRecord updated = result.Value.Single(i => i.TypeId == typeId);
        updated.Quantity.Should().Be(500);
        updated.Price.Should().BeApproximately(35.0, 0.001);
    }

    [Fact]
    public async Task DeleteItemAsync_ExistingItem_ReturnsTrue()
    {
        TypeId typeId = new(38);
        await _sut.UpsertItemAsync(new ShoppingListItemRecord(typeId, "Nocxium", 100, 500.0));

        Result<bool> result = await _sut.DeleteItemAsync(typeId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteItemAsync_MissingItem_ReturnsFalse()
    {
        Result<bool> result = await _sut.DeleteItemAsync(new TypeId(999_888));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task ClearAsync_RemovesAllItems()
    {
        await _sut.UpsertItemAsync(new ShoppingListItemRecord(new TypeId(40), "Zydrine", 10, 1000.0));
        await _sut.UpsertItemAsync(new ShoppingListItemRecord(new TypeId(41), "Megacyte", 5, 2000.0));

        Result<bool> result = await _sut.ClearAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        Result<IReadOnlyList<ShoppingListItemRecord>> items = await _sut.GetItemsAsync();
        items.Value.Should().BeEmpty();
    }
}
