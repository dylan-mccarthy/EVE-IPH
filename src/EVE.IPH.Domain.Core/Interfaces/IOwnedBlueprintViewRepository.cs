using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

public interface IOwnedBlueprintViewRepository
{
    Task<Result<IReadOnlyList<OwnedBlueprintViewRecord>>> GetByOwnersAsync(
        IReadOnlyList<long> ownerIds,
        CancellationToken cancellationToken = default);
}

public sealed record OwnedBlueprintViewRecord(
    long OwnerId,
    string OwnerName,
    bool IsCorporationOwner,
    long ItemId,
    long LocationId,
    long BlueprintId,
    string BlueprintName,
    int Quantity,
    int Me,
    int Te,
    int Runs,
    int BpType,
    bool Owned,
    bool Scanned);