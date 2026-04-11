using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.ShoppingList.Models;

namespace EVE.IPH.Domain.ShoppingList.Services;

public sealed class ShoppingListService(
    IShoppingListRepository shoppingListRepository,
    ShoppingListAggregator aggregator) : IShoppingListService
{
    private readonly IShoppingListRepository _shoppingListRepository = shoppingListRepository ?? throw new ArgumentNullException(nameof(shoppingListRepository));
    private readonly ShoppingListAggregator _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));

    public async Task<Result<AggregatedShoppingList>> GetPersistedAsync(CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<ShoppingListItemRecord>> records = await _shoppingListRepository.GetItemsAsync(cancellationToken).ConfigureAwait(false);
        if (records.IsFailure)
        {
            return Result<AggregatedShoppingList>.Failure(records.Error);
        }

        AggregatedShoppingList list = _aggregator.Empty;
        foreach (ShoppingListItemRecord record in records.Value)
        {
            Result<AggregatedShoppingList> appended = _aggregator.AddItem(list, new ShoppingListLineItem(
                record.TypeId,
                record.ItemName,
                "Buy",
                record.Quantity,
                0d,
                record.Price,
                Kind: ShoppingListLineItemKind.Buy));
            if (appended.IsFailure)
            {
                return Result<AggregatedShoppingList>.Failure(appended.Error);
            }

            list = appended.Value;
        }

        return Result<AggregatedShoppingList>.Success(list);
    }

    public async Task<Result<AggregatedShoppingList>> SaveAsync(AggregatedShoppingList list, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(list);

        if (HasDuplicateTypeIds(list))
        {
            return Result<AggregatedShoppingList>.Failure(
                "DUPLICATE_TYPE_ID_ROWS",
                "Persisted shopping list rows must have unique type IDs because the current repository schema keys on TYPE_ID.");
        }

        Result<bool> cleared = await _shoppingListRepository.ClearAsync(cancellationToken).ConfigureAwait(false);
        if (cleared.IsFailure)
        {
            return Result<AggregatedShoppingList>.Failure(cleared.Error);
        }

        foreach (ShoppingListLineItem item in list.Items)
        {
            Result<ShoppingListItemRecord> saved = await _shoppingListRepository.UpsertItemAsync(
                new ShoppingListItemRecord(item.TypeId, item.ItemName, item.Quantity, item.UnitPrice),
                cancellationToken).ConfigureAwait(false);
            if (saved.IsFailure)
            {
                return Result<AggregatedShoppingList>.Failure(saved.Error);
            }
        }

        return Result<AggregatedShoppingList>.Success(list);
    }

    public Result<ShoppingListProjection> Project(
        AggregatedShoppingList list,
        IReadOnlyCollection<ShoppingListLineItem>? onHandMaterials = null,
        IReadOnlyCollection<ShoppingListLineItem>? onHandBuildItems = null)
    {
        ArgumentNullException.ThrowIfNull(list);

        List<ShoppingListLineItem> buyItems = list.Items.Where(item => item.Kind == ShoppingListLineItemKind.Buy).ToList();
        List<ShoppingListLineItem> buildItems = list.Items.Where(item => item.Kind == ShoppingListLineItemKind.Build || item.IsBuildItem).ToList();
        List<ShoppingListLineItem> inventionItems = list.Items.Where(item => item.Kind == ShoppingListLineItemKind.Invention).ToList();
        List<ShoppingListLineItem> copyItems = list.Items.Where(item => item.Kind == ShoppingListLineItemKind.Copy).ToList();
        List<ShoppingListLineItem> finalItems = list.Items.Where(item => item.Kind == ShoppingListLineItemKind.FinalItem).ToList();

        Result<AggregatedShoppingList> remainingBuyItems = SubtractOnHand(
            CreateSnapshot(buyItems),
            onHandMaterials ?? []);
        if (remainingBuyItems.IsFailure)
        {
            return Result<ShoppingListProjection>.Failure(remainingBuyItems.Error);
        }

        Result<AggregatedShoppingList> remainingBuildItems = SubtractOnHand(
            CreateSnapshot(buildItems),
            onHandBuildItems ?? []);
        if (remainingBuildItems.IsFailure)
        {
            return Result<ShoppingListProjection>.Failure(remainingBuildItems.Error);
        }

        return Result<ShoppingListProjection>.Success(new ShoppingListProjection(
            list,
            CreateSnapshot(buyItems),
            CreateSnapshot(buildItems),
            CreateSnapshot(inventionItems),
            CreateSnapshot(copyItems),
            CreateSnapshot(finalItems),
            remainingBuyItems.Value,
            remainingBuildItems.Value));
    }

    private Result<AggregatedShoppingList> SubtractOnHand(
        AggregatedShoppingList source,
        IReadOnlyCollection<ShoppingListLineItem> onHandItems)
    {
        AggregatedShoppingList current = source;
        foreach (ShoppingListLineItem onHandItem in onHandItems)
        {
            Result<AggregatedShoppingList> updated = TryRemoveIfPresent(current, onHandItem);
            if (updated.IsFailure)
            {
                return updated;
            }

            current = updated.Value;
        }

        return Result<AggregatedShoppingList>.Success(current);
    }

    private Result<AggregatedShoppingList> TryRemoveIfPresent(AggregatedShoppingList list, ShoppingListLineItem item)
    {
        bool exists = list.Items.Any(existing => MatchesLegacyIdentity(existing, item));
        if (!exists)
        {
            return Result<AggregatedShoppingList>.Success(list);
        }

        return _aggregator.RemoveItem(list, item);
    }

    private static bool HasDuplicateTypeIds(AggregatedShoppingList list) =>
        list.Items.GroupBy(item => item.TypeId).Any(group => group.Count() > 1);

    private static bool MatchesLegacyIdentity(ShoppingListLineItem left, ShoppingListLineItem right) =>
        string.Equals(left.ItemName, right.ItemName, StringComparison.Ordinal)
        && string.Equals(left.GroupName, right.GroupName, StringComparison.Ordinal)
        && string.Equals(left.MaterialEfficiency, right.MaterialEfficiency, StringComparison.Ordinal)
        && left.Kind == right.Kind;

    private static AggregatedShoppingList CreateSnapshot(IReadOnlyCollection<ShoppingListLineItem> items) => new(
        items.ToList(),
        items.Sum(item => item.TotalCost),
        items.Sum(item => item.TotalVolume));
}