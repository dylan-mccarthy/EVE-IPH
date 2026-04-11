namespace EVE.IPH.Domain.ShoppingList.Models;

public sealed record ShoppingListProjection(
    AggregatedShoppingList AllItems,
    AggregatedShoppingList BuyItems,
    AggregatedShoppingList BuildItems,
    AggregatedShoppingList InventionItems,
    AggregatedShoppingList CopyItems,
    AggregatedShoppingList FinalItems,
    AggregatedShoppingList RemainingBuyItems,
    AggregatedShoppingList RemainingBuildItems);