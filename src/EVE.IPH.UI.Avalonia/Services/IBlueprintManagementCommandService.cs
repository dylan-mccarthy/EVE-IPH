using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IBlueprintManagementCommandService
{
    Task<BlueprintManagementScreenData> RefreshAsync(CancellationToken cancellationToken = default);

    Task<Result<OwnedBlueprintRecord>> SaveBlueprintAsync(OwnedBlueprintRecord blueprint, CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteBlueprintAsync(long ownerId, BlueprintId blueprintId, CancellationToken cancellationToken = default);
}