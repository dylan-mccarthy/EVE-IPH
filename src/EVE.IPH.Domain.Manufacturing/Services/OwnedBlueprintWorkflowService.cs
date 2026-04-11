using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class OwnedBlueprintWorkflowService(
    IOwnedBlueprintRepository ownedBlueprintRepository,
    IOwnedBlueprintViewRepository ownedBlueprintViewRepository) : IOwnedBlueprintWorkflowService
{
    private readonly IOwnedBlueprintRepository _ownedBlueprintRepository = ownedBlueprintRepository ?? throw new ArgumentNullException(nameof(ownedBlueprintRepository));
    private readonly IOwnedBlueprintViewRepository _ownedBlueprintViewRepository = ownedBlueprintViewRepository ?? throw new ArgumentNullException(nameof(ownedBlueprintViewRepository));

    public Task<Result<IReadOnlyList<OwnedBlueprintViewRecord>>> GetBlueprintsByOwnersAsync(
        IReadOnlyList<long> ownerIds,
        CancellationToken cancellationToken = default) =>
        _ownedBlueprintViewRepository.GetByOwnersAsync(ownerIds, cancellationToken);

    public Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> ReplaceBlueprintsAsync(
        long ownerId,
        IReadOnlyList<OwnedBlueprintRecord> blueprints,
        CancellationToken cancellationToken = default) =>
        _ownedBlueprintRepository.ReplaceAsync(ownerId, blueprints, cancellationToken);

    public Task<Result<OwnedBlueprintRecord>> SaveBlueprintAsync(
        OwnedBlueprintRecord blueprint,
        CancellationToken cancellationToken = default) =>
        _ownedBlueprintRepository.UpsertAsync(blueprint, cancellationToken);

    public Task<Result<bool>> DeleteBlueprintAsync(
        long ownerId,
        BlueprintId blueprintId,
        CancellationToken cancellationToken = default) =>
        _ownedBlueprintRepository.DeleteAsync(ownerId, blueprintId, cancellationToken);
}