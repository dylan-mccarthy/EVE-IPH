using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IShoppingListWorkspaceCommandService
{
    Task<Result<ShoppingListScreenData>> ClearAsync(CancellationToken cancellationToken = default);

    Task<Result<ShoppingListScreenData>> RemoveItemAsync(long typeId, CancellationToken cancellationToken = default);
}