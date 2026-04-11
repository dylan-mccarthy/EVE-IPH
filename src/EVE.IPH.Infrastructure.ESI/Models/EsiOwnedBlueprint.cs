using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Infrastructure.ESI.Models;

public sealed record EsiOwnedBlueprint(
    long OwnerId,
    ItemId ItemId,
    long LocationId,
    BlueprintId BlueprintId,
    int Quantity,
    int Me,
    int Te,
    int Runs);