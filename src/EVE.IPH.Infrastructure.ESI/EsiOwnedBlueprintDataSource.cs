using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Interfaces;
using EVE.IPH.Infrastructure.ESI.Models;

namespace EVE.IPH.Infrastructure.ESI;

public sealed class EsiOwnedBlueprintDataSource(
    IEsiClient esiClient,
    IItemRepository itemRepository) : IOwnedBlueprintDataSource
{
    private const int BlueprintTypeOriginal = -1;
    private const int BlueprintTypeCopy = -2;
    private const int OwnedBlueprintOriginal = 1;
    private const int OwnedBlueprintCopy = 2;

    private readonly IEsiClient _esiClient = esiClient ?? throw new ArgumentNullException(nameof(esiClient));
    private readonly IItemRepository _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));

    public async Task<Result<IReadOnlyList<OwnedBlueprintData>>> GetCorporationBlueprintsAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<EsiOwnedBlueprint>> blueprints = await _esiClient
            .GetCorporationBlueprintsAsync(corporationId, authenticatedCharacterId, cancellationToken)
            .ConfigureAwait(false);

        if (blueprints.IsFailure)
        {
            return Result<IReadOnlyList<OwnedBlueprintData>>.Failure(blueprints.Error);
        }

        List<OwnedBlueprintData> mappedBlueprints = new(blueprints.Value.Count);

        foreach (EsiOwnedBlueprint blueprint in blueprints.Value)
        {
            Maybe<string> blueprintName = await _itemRepository.GetItemNameAsync(new TypeId(blueprint.BlueprintId.Value), cancellationToken).ConfigureAwait(false);
            mappedBlueprints.Add(new OwnedBlueprintData(
                blueprint.OwnerId,
                blueprint.ItemId,
                blueprint.LocationId,
                blueprint.BlueprintId,
                blueprintName.HasValue ? blueprintName.Value : blueprint.BlueprintId.Value.ToString(),
                NormalizeQuantity(blueprint.Quantity),
                blueprint.Me,
                blueprint.Te,
                blueprint.Runs,
                MapBlueprintType(blueprint.Quantity),
                true,
                true));
        }

        return Result<IReadOnlyList<OwnedBlueprintData>>.Success(mappedBlueprints);
    }

    private static int MapBlueprintType(int quantity) => quantity switch
    {
        BlueprintTypeCopy => OwnedBlueprintCopy,
        BlueprintTypeOriginal => OwnedBlueprintOriginal,
        > 0 => OwnedBlueprintOriginal,
        _ => 0,
    };

    private static int NormalizeQuantity(int quantity) => quantity > 0 ? quantity : 1;
}