using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.ShoppingList.Models;
using EVE.IPH.Domain.ShoppingList.Services;

namespace EVE.IPH.Domain.ShoppingList.Tests.Services;

public sealed class ShoppingListAggregatorTests
{
    [Fact]
    public void AddItem_WhenListIsEmpty_AddsTheItemAndUpdatesTotals()
    {
        ShoppingListAggregator sut = new();
        ShoppingListLineItem item = CreateItem(quantity: 10, unitVolume: 0.5d, unitPrice: 12.5d);

        var result = sut.AddItem(sut.Empty, item);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.TotalCost.Should().Be(125d);
        result.Value.TotalVolume.Should().Be(5d);
    }

    [Fact]
    public void AddItem_WhenLegacyIdentityMatches_MergesQuantitiesInsteadOfAddingAnotherRow()
    {
        ShoppingListAggregator sut = new();
        var first = sut.AddItem(sut.Empty, CreateItem(quantity: 4, unitVolume: 1.2d, unitPrice: 5d)).Value;

        var result = sut.AddItem(first, CreateItem(quantity: 6, unitVolume: 1.2d, unitPrice: 5d, typeId: 9999));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Quantity.Should().Be(10);
        result.Value.TotalCost.Should().Be(50d);
        result.Value.TotalVolume.Should().Be(12d);
    }

    [Fact]
    public void AddItem_WhenMaterialEfficiencyDiffers_KeepsSeparateRows()
    {
        ShoppingListAggregator sut = new();
        var first = sut.AddItem(sut.Empty, CreateItem(quantity: 4, materialEfficiency: "10")).Value;

        var result = sut.AddItem(first, CreateItem(quantity: 6, materialEfficiency: "8"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public void AddItem_WhenKindsDiffer_KeepsSeparateRows()
    {
        ShoppingListAggregator sut = new();
        var first = sut.AddItem(sut.Empty, CreateItem(quantity: 4, kind: ShoppingListLineItemKind.Buy)).Value;

        var result = sut.AddItem(first, CreateItem(quantity: 6, kind: ShoppingListLineItemKind.Invention));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public void RemoveItem_WhenQuantityRemains_ReducesQuantityAndRecalculatesTotals()
    {
        ShoppingListAggregator sut = new();
        var list = sut.AddItem(sut.Empty, CreateItem(quantity: 10, unitVolume: 2d, unitPrice: 4d)).Value;

        var result = sut.RemoveItem(list, CreateItem(quantity: 3, unitVolume: 2d, unitPrice: 4d));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Quantity.Should().Be(7);
        result.Value.TotalCost.Should().Be(28d);
        result.Value.TotalVolume.Should().Be(14d);
    }

    [Fact]
    public void RemoveItem_WhenQuantityIsFullyRemoved_DeletesTheRow()
    {
        ShoppingListAggregator sut = new();
        var list = sut.AddItem(sut.Empty, CreateItem(quantity: 5, unitVolume: 3d, unitPrice: 8d)).Value;

        var result = sut.RemoveItem(list, CreateItem(quantity: 5, unitVolume: 3d, unitPrice: 8d));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCost.Should().Be(0d);
        result.Value.TotalVolume.Should().Be(0d);
    }

    [Fact]
    public void AddItem_WhenQuantityIsInvalid_ReturnsFailure()
    {
        ShoppingListAggregator sut = new();

        var result = sut.AddItem(sut.Empty, CreateItem(quantity: 0));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_QUANTITY");
    }

    [Fact]
    public async Task Service_GetPersistedAsync_LoadsRepositoryRowsIntoAggregate()
    {
        InMemoryShoppingListRepository repository = new();
        await repository.UpsertItemAsync(new(new TypeId(34), "Tritanium", 100, 5d));
        await repository.UpsertItemAsync(new(new TypeId(35), "Pyerite", 200, 7d));
        ShoppingListService sut = new(repository, new ShoppingListAggregator());

        var result = await sut.GetPersistedAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCost.Should().Be(1900d);
    }

    [Fact]
    public async Task Service_SaveAsync_WhenAggregateContainsDuplicateTypeIds_ReturnsFailure()
    {
        ShoppingListAggregator aggregator = new();
        InMemoryShoppingListRepository repository = new();
        ShoppingListService sut = new(repository, aggregator);
        var list = aggregator.AddItem(aggregator.Empty, CreateItem(quantity: 5, typeId: 34, materialEfficiency: "10")).Value;
        list = aggregator.AddItem(list, CreateItem(quantity: 2, typeId: 34, materialEfficiency: "8")).Value;

        var result = await sut.SaveAsync(list);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DUPLICATE_TYPE_ID_ROWS");
    }

    [Fact]
    public async Task Service_SaveAsync_ReplacesPersistedRowsWithCurrentAggregate()
    {
        ShoppingListAggregator aggregator = new();
        InMemoryShoppingListRepository repository = new();
        ShoppingListService sut = new(repository, aggregator);
        var list = aggregator.AddItem(aggregator.Empty, CreateItem(quantity: 5, typeId: 34, unitPrice: 2d)).Value;
        list = aggregator.AddItem(list, CreateItem(quantity: 3, typeId: 35, itemName: "Pyerite", unitPrice: 4d)).Value;

        var result = await sut.SaveAsync(list);

        result.IsSuccess.Should().BeTrue();
        repository.Items.Should().HaveCount(2);
        repository.Items.Should().ContainSingle(item => item.TypeId.Value == 34 && item.Quantity == 5);
        repository.Items.Should().ContainSingle(item => item.TypeId.Value == 35 && item.Quantity == 3);
    }

    [Fact]
    public void Service_Project_SplitsBuildAndBuyItemsAndSubtractsOnHandLists()
    {
        ShoppingListAggregator aggregator = new();
        ShoppingListService sut = new(new InMemoryShoppingListRepository(), aggregator);
        var list = aggregator.AddItem(aggregator.Empty, CreateItem(quantity: 10, typeId: 34, itemName: "Tritanium", kind: ShoppingListLineItemKind.Buy)).Value;
        list = aggregator.AddItem(list, CreateItem(quantity: 4, typeId: 44992, itemName: "Capital Core Temperature Regulator", groupName: "Built Item", isBuildItem: true, unitPrice: 100d, kind: ShoppingListLineItemKind.Build)).Value;

        var result = sut.Project(
            list,
            [CreateItem(quantity: 3, typeId: 34, itemName: "Tritanium", kind: ShoppingListLineItemKind.Buy)],
            [CreateItem(quantity: 1, typeId: 44992, itemName: "Capital Core Temperature Regulator", groupName: "Built Item", isBuildItem: true, unitPrice: 100d, kind: ShoppingListLineItemKind.Build)]);

        result.IsSuccess.Should().BeTrue();
        result.Value.BuyItems.Items.Should().ContainSingle();
        result.Value.BuildItems.Items.Should().ContainSingle();
        result.Value.RemainingBuyItems.Items[0].Quantity.Should().Be(7);
        result.Value.RemainingBuildItems.Items[0].Quantity.Should().Be(3);
    }

    [Fact]
    public void Service_Project_ReturnsInventionCopyAndFinalItemViews()
    {
        ShoppingListAggregator aggregator = new();
        ShoppingListService sut = new(new InMemoryShoppingListRepository(), aggregator);
        var list = aggregator.AddItem(aggregator.Empty, CreateItem(quantity: 8, typeId: 20424, itemName: "Datacore - Electronic Engineering", kind: ShoppingListLineItemKind.Invention)).Value;
        list = aggregator.AddItem(list, CreateItem(quantity: 2, typeId: 30013, itemName: "R.Db - Incognito Ship Data Interfaces", kind: ShoppingListLineItemKind.Copy)).Value;
        list = aggregator.AddItem(list, CreateItem(quantity: 5, typeId: 12005, itemName: "Ishtar", groupName: "Build|None|1|None|Sotiyo", kind: ShoppingListLineItemKind.FinalItem)).Value;

        var result = sut.Project(list);

        result.IsSuccess.Should().BeTrue();
        result.Value.InventionItems.Items.Should().ContainSingle();
        result.Value.CopyItems.Items.Should().ContainSingle();
        result.Value.FinalItems.Items.Should().ContainSingle();
        result.Value.InventionItems.Items[0].ItemName.Should().Be("Datacore - Electronic Engineering");
        result.Value.CopyItems.Items[0].ItemName.Should().Be("R.Db - Incognito Ship Data Interfaces");
        result.Value.FinalItems.Items[0].ItemName.Should().Be("Ishtar");
    }

    private static ShoppingListLineItem CreateItem(
        long quantity,
        double unitVolume = 1d,
        double unitPrice = 2d,
        string materialEfficiency = "10",
        long typeId = 34,
        string itemName = "Tritanium",
        string groupName = "Jita",
        bool isBuildItem = false,
        ShoppingListLineItemKind kind = ShoppingListLineItemKind.Buy) => new(
            new TypeId(typeId),
            itemName,
            groupName,
            quantity,
            unitVolume,
            unitPrice,
            materialEfficiency,
            TimeEfficiency: "20",
            IsBuildItem: isBuildItem,
            Kind: kind);

    private sealed class InMemoryShoppingListRepository : EVE.IPH.Domain.Core.Interfaces.IShoppingListRepository
    {
        private readonly List<EVE.IPH.Domain.Core.Interfaces.ShoppingListItemRecord> _items = [];

        public IReadOnlyList<EVE.IPH.Domain.Core.Interfaces.ShoppingListItemRecord> Items => _items;

        public Task<EVE.IPH.Domain.Core.Results.Result<bool>> ClearAsync(CancellationToken cancellationToken = default)
        {
            _items.Clear();
            return Task.FromResult(EVE.IPH.Domain.Core.Results.Result<bool>.Success(true));
        }

        public Task<EVE.IPH.Domain.Core.Results.Result<bool>> DeleteItemAsync(TypeId typeId, CancellationToken cancellationToken = default)
        {
            bool removed = _items.RemoveAll(item => item.TypeId == typeId) > 0;
            return Task.FromResult(EVE.IPH.Domain.Core.Results.Result<bool>.Success(removed));
        }

        public Task<EVE.IPH.Domain.Core.Results.Result<IReadOnlyList<EVE.IPH.Domain.Core.Interfaces.ShoppingListItemRecord>>> GetItemsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(EVE.IPH.Domain.Core.Results.Result<IReadOnlyList<EVE.IPH.Domain.Core.Interfaces.ShoppingListItemRecord>>.Success(_items.ToList()));
        }

        public Task<EVE.IPH.Domain.Core.Results.Result<EVE.IPH.Domain.Core.Interfaces.ShoppingListItemRecord>> UpsertItemAsync(EVE.IPH.Domain.Core.Interfaces.ShoppingListItemRecord record, CancellationToken cancellationToken = default)
        {
            int existingIndex = _items.FindIndex(item => item.TypeId == record.TypeId);
            if (existingIndex >= 0)
            {
                _items[existingIndex] = record;
            }
            else
            {
                _items.Add(record);
            }

            return Task.FromResult(EVE.IPH.Domain.Core.Results.Result<EVE.IPH.Domain.Core.Interfaces.ShoppingListItemRecord>.Success(record));
        }
    }
}