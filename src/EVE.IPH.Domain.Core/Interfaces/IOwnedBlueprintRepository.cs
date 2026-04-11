using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Persists and retrieves user-owned blueprint records from the application database.
/// </summary>
public interface IOwnedBlueprintRepository
{
    Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> GetByUserAsync(long userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> GetByUsersAsync(IReadOnlyList<long> userIds, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> ReplaceAsync(long userId, IReadOnlyList<OwnedBlueprintRecord> blueprints, CancellationToken cancellationToken = default);
    Task<Result<OwnedBlueprintRecord>> UpsertAsync(OwnedBlueprintRecord record, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(long userId, BlueprintId blueprintId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteByUserAsync(long userId, CancellationToken cancellationToken = default);
}

/// <summary>A user-owned blueprint entry in the application database.</summary>
public sealed record OwnedBlueprintRecord(
    long UserId,
    ItemId ItemId,
    long LocationId,
    BlueprintId BlueprintId,
    string BlueprintName,
    int Quantity,
    int Me,
    int Te,
    int Runs,
    int BpType,
    bool Owned,
    bool Scanned);
