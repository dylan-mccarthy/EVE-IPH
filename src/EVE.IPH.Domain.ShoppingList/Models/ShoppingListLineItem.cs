using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.ShoppingList.Models;

public sealed record ShoppingListLineItem(
    TypeId TypeId,
    string ItemName,
    string GroupName,
    long Quantity,
    double UnitVolume,
    double UnitPrice,
    string MaterialEfficiency = "-",
    string TimeEfficiency = "-",
    bool IsBuildItem = false,
    ShoppingListLineItemKind Kind = ShoppingListLineItemKind.Buy)
{
    public double TotalVolume => UnitVolume * Quantity;

    public double TotalCost => UnitPrice * Quantity;
}