using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Persists and retrieves stored character-owned assets.
/// </summary>
public interface IAssetRepository
{
    Task<Result<IReadOnlyList<StoredAssetRecord>>> GetByOwnerIdAsync(
        long ownerId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<StoredAssetRecord>>> ReplaceAsync(
        long ownerId,
        IReadOnlyList<StoredAssetRecord> assets,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteByOwnerIdAsync(
        long ownerId,
        CancellationToken cancellationToken = default);
}

/// <summary>A stored asset row.</summary>
public sealed record StoredAssetRecord(
    long OwnerId,
    long ItemId,
    long LocationId,
    TypeId TypeId,
    long Quantity,
    int FlagId,
    bool IsSingleton,
    bool IsBlueprintCopy,
    string ItemName);