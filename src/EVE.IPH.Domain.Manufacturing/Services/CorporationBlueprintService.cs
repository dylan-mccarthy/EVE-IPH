using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class CorporationBlueprintService(
    IOwnedBlueprintRepository ownedBlueprintRepository,
    IOwnedBlueprintDataSource ownedBlueprintDataSource) : ICorporationBlueprintService
{
    private readonly IOwnedBlueprintRepository _ownedBlueprintRepository = ownedBlueprintRepository ?? throw new ArgumentNullException(nameof(ownedBlueprintRepository));
    private readonly IOwnedBlueprintDataSource _ownedBlueprintDataSource = ownedBlueprintDataSource ?? throw new ArgumentNullException(nameof(ownedBlueprintDataSource));

    public Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> GetAsync(
        CorporationId corporationId,
        CancellationToken cancellationToken = default) =>
        _ownedBlueprintRepository.GetByUserAsync(corporationId.Value, cancellationToken);

    public async Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> RefreshAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<OwnedBlueprintData>> currentBlueprints = await _ownedBlueprintDataSource
            .GetCorporationBlueprintsAsync(corporationId, authenticatedCharacterId, cancellationToken)
            .ConfigureAwait(false);

        if (currentBlueprints.IsFailure)
        {
            return Result<IReadOnlyList<OwnedBlueprintRecord>>.Failure(currentBlueprints.Error);
        }

        IReadOnlyList<OwnedBlueprintRecord> normalizedBlueprints = currentBlueprints.Value
            .Select(blueprint => new OwnedBlueprintRecord(
                blueprint.OwnerId,
                blueprint.ItemId,
                blueprint.LocationId,
                blueprint.BlueprintId,
                blueprint.BlueprintName,
                blueprint.Quantity,
                blueprint.Me,
                blueprint.Te,
                blueprint.Runs,
                blueprint.BpType,
                blueprint.Owned,
                blueprint.Scanned))
            .ToArray();

        return await _ownedBlueprintRepository
            .ReplaceAsync(corporationId.Value, normalizedBlueprints, cancellationToken)
            .ConfigureAwait(false);
    }
}