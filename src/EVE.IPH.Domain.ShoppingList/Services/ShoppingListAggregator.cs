using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.ShoppingList.Models;

namespace EVE.IPH.Domain.ShoppingList.Services;

public sealed class ShoppingListAggregator
{
    public AggregatedShoppingList Empty { get; } = new([], 0d, 0d);

    public Result<AggregatedShoppingList> AddItem(AggregatedShoppingList list, ShoppingListLineItem item)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(item);

        if (item.Quantity <= 0)
        {
            return Result<AggregatedShoppingList>.Failure("INVALID_QUANTITY", "Item quantity must be greater than zero.");
        }

        List<ShoppingListLineItem> items = list.Items.ToList();
        int existingIndex = items.FindIndex(existing => MatchesLegacyIdentity(existing, item));
        if (existingIndex >= 0)
        {
            ShoppingListLineItem existing = items[existingIndex];
            items[existingIndex] = existing with { Quantity = existing.Quantity + item.Quantity };
        }
        else
        {
            items.Add(item);
        }

        return Result<AggregatedShoppingList>.Success(CreateSnapshot(items));
    }

    public Result<AggregatedShoppingList> RemoveItem(AggregatedShoppingList list, ShoppingListLineItem item)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(item);

        if (item.Quantity <= 0)
        {
            return Result<AggregatedShoppingList>.Failure("INVALID_QUANTITY", "Item quantity must be greater than zero.");
        }

        List<ShoppingListLineItem> items = list.Items.ToList();
        int existingIndex = items.FindIndex(existing => MatchesLegacyIdentity(existing, item));
        if (existingIndex < 0)
        {
            return Result<AggregatedShoppingList>.Failure("ITEM_NOT_FOUND", "The requested item was not found in the shopping list.");
        }

        ShoppingListLineItem existing = items[existingIndex];
        long updatedQuantity = existing.Quantity - item.Quantity;
        if (updatedQuantity > 0)
        {
            items[existingIndex] = existing with { Quantity = updatedQuantity };
        }
        else
        {
            items.RemoveAt(existingIndex);
        }

        return Result<AggregatedShoppingList>.Success(CreateSnapshot(items));
    }

    private static bool MatchesLegacyIdentity(ShoppingListLineItem left, ShoppingListLineItem right) =>
        string.Equals(left.ItemName, right.ItemName, StringComparison.Ordinal)
        && string.Equals(left.GroupName, right.GroupName, StringComparison.Ordinal)
        && string.Equals(left.MaterialEfficiency, right.MaterialEfficiency, StringComparison.Ordinal)
        && left.Kind == right.Kind;

    private static AggregatedShoppingList CreateSnapshot(List<ShoppingListLineItem> items)
    {
        double totalCost = items.Sum(item => item.TotalCost);
        double totalVolume = items.Sum(item => item.TotalVolume);

        return new AggregatedShoppingList(items, totalCost, totalVolume);
    }
}