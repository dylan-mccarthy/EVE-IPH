using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.ShoppingList.Models;
using EVE.IPH.Domain.ShoppingList.Services;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class ShoppingListWorkspaceQueryService(IShoppingListService shoppingListService) : IShoppingListWorkspaceQueryService
{
    private readonly IShoppingListService _shoppingListService = shoppingListService ?? throw new ArgumentNullException(nameof(shoppingListService));

    public async Task<ShoppingListScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default)
    {
        Result<AggregatedShoppingList> persistedResult = await _shoppingListService.GetPersistedAsync(cancellationToken).ConfigureAwait(false);
        if (persistedResult.IsFailure)
        {
            return new ShoppingListScreenData([], 0, 0, 0d, $"Unable to load the persisted shopping list: {persistedResult.Error.Message}");
        }

        Result<ShoppingListProjection> projectionResult = _shoppingListService.Project(persistedResult.Value);
        if (projectionResult.IsFailure)
        {
            return new ShoppingListScreenData([], 0, 0, 0d, $"Unable to project the persisted shopping list: {projectionResult.Error.Message}");
        }

        IReadOnlyList<ShoppingListRow> rows = projectionResult.Value.AllItems.Items
            .OrderBy(item => item.ItemName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.TypeId.Value)
            .Select(item => new ShoppingListRow(item.TypeId.Value, item.ItemName, item.Quantity, item.UnitPrice))
            .ToArray();

        if (rows.Count == 0)
        {
            return new ShoppingListScreenData(
                rows,
                0,
                0,
                0d,
                "No persisted shopping-list rows were found yet. This first shell slice reflects the current repository-backed list shape; richer build/buy generation and export workflows still follow later.");
        }

        long totalQuantity = projectionResult.Value.AllItems.Items.Sum(item => item.Quantity);
        double totalCost = projectionResult.Value.AllItems.TotalCost;

        return new ShoppingListScreenData(
            rows,
            rows.Count,
            totalQuantity,
            totalCost,
            $"Loaded {rows.Count} persisted shopping-list row{(rows.Count == 1 ? string.Empty : "s")} from the local SQLite store. This first shell slice reflects the current repository-backed list shape; richer build/buy generation and export workflows remain deferred.");
    }
}