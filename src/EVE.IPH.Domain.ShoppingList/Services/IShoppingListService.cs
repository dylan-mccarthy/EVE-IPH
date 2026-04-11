using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.ShoppingList.Models;

namespace EVE.IPH.Domain.ShoppingList.Services;

public interface IShoppingListService
{
    Task<Result<AggregatedShoppingList>> GetPersistedAsync(CancellationToken cancellationToken = default);

    Task<Result<AggregatedShoppingList>> SaveAsync(AggregatedShoppingList list, CancellationToken cancellationToken = default);

    Result<ShoppingListProjection> Project(
        AggregatedShoppingList list,
        IReadOnlyCollection<ShoppingListLineItem>? onHandMaterials = null,
        IReadOnlyCollection<ShoppingListLineItem>? onHandBuildItems = null);
}