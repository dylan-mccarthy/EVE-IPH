using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class ShoppingListWorkspaceCommandService(
    IShoppingListRepository shoppingListRepository,
    IShoppingListWorkspaceQueryService shoppingListWorkspaceQueryService) : IShoppingListWorkspaceCommandService
{
    private readonly IShoppingListRepository _shoppingListRepository = shoppingListRepository ?? throw new ArgumentNullException(nameof(shoppingListRepository));
    private readonly IShoppingListWorkspaceQueryService _shoppingListWorkspaceQueryService = shoppingListWorkspaceQueryService ?? throw new ArgumentNullException(nameof(shoppingListWorkspaceQueryService));

    public async Task<Result<ShoppingListScreenData>> ClearAsync(CancellationToken cancellationToken = default)
    {
        Result<bool> clearResult = await _shoppingListRepository.ClearAsync(cancellationToken).ConfigureAwait(false);
        if (clearResult.IsFailure)
        {
            return Result<ShoppingListScreenData>.Failure(clearResult.Error);
        }

        ShoppingListScreenData screenData = await _shoppingListWorkspaceQueryService.GetScreenDataAsync(cancellationToken).ConfigureAwait(false);
        return Result<ShoppingListScreenData>.Success(screenData);
    }

    public async Task<Result<ShoppingListScreenData>> RemoveItemAsync(long typeId, CancellationToken cancellationToken = default)
    {
        if (typeId <= 0)
        {
            return Result<ShoppingListScreenData>.Failure("INVALID_TYPE_ID", "Shopping-list item IDs must be greater than zero.");
        }

        Result<bool> deleteResult = await _shoppingListRepository.DeleteItemAsync(new TypeId(typeId), cancellationToken).ConfigureAwait(false);
        if (deleteResult.IsFailure)
        {
            return Result<ShoppingListScreenData>.Failure(deleteResult.Error);
        }

        ShoppingListScreenData screenData = await _shoppingListWorkspaceQueryService.GetScreenDataAsync(cancellationToken).ConfigureAwait(false);
        return Result<ShoppingListScreenData>.Success(screenData);
    }
}