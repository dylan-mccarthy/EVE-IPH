using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Persists and retrieves shopping list items from the application database.
/// </summary>
public interface IShoppingListRepository
{
    Task<Result<IReadOnlyList<ShoppingListItemRecord>>> GetItemsAsync(CancellationToken cancellationToken = default);
    Task<Result<ShoppingListItemRecord>> UpsertItemAsync(ShoppingListItemRecord record, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteItemAsync(TypeId typeId, CancellationToken cancellationToken = default);
    Task<Result<bool>> ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>A single item in the shopping list.</summary>
public sealed record ShoppingListItemRecord(
    TypeId TypeId,
    string ItemName,
    long Quantity,
    double Price);
