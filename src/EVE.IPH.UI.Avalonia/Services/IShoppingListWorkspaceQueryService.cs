namespace EVE.IPH.UI.Avalonia.Services;

public interface IShoppingListWorkspaceQueryService
{
    Task<ShoppingListScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default);
}

public sealed record ShoppingListScreenData(
    IReadOnlyList<ShoppingListRow> Items,
    int ItemCount,
    long TotalQuantity,
    double TotalCost,
    string StatusText);

public sealed record ShoppingListRow(
    long TypeId,
    string ItemName,
    long Quantity,
    double UnitPrice)
{
    public double TotalPrice => Quantity * UnitPrice;
}