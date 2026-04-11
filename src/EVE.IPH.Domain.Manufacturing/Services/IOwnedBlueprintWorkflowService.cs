using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Manufacturing.Services;

public interface IOwnedBlueprintWorkflowService
{
    Task<Result<IReadOnlyList<OwnedBlueprintViewRecord>>> GetBlueprintsByOwnersAsync(
        IReadOnlyList<long> ownerIds,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> ReplaceBlueprintsAsync(
        long ownerId,
        IReadOnlyList<OwnedBlueprintRecord> blueprints,
        CancellationToken cancellationToken = default);

    Task<Result<OwnedBlueprintRecord>> SaveBlueprintAsync(
        OwnedBlueprintRecord blueprint,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteBlueprintAsync(
        long ownerId,
        BlueprintId blueprintId,
        CancellationToken cancellationToken = default);
}