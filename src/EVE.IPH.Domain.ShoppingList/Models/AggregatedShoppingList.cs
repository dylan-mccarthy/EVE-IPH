namespace EVE.IPH.Domain.ShoppingList.Models;

public sealed record AggregatedShoppingList(
    IReadOnlyList<ShoppingListLineItem> Items,
    double TotalCost,
    double TotalVolume);