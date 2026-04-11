using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Fetches owned blueprint data from an external source.
/// </summary>
public interface IOwnedBlueprintDataSource
{
    Task<Result<IReadOnlyList<OwnedBlueprintData>>> GetCorporationBlueprintsAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        CancellationToken cancellationToken = default);
}

/// <summary>Blueprint ownership data returned by an external source.</summary>
public sealed record OwnedBlueprintData(
    long OwnerId,
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