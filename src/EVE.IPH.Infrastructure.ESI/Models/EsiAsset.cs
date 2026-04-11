using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Infrastructure.ESI.Models;

public sealed record EsiAsset(
    long OwnerId,
    long ItemId,
    long LocationId,
    TypeId TypeId,
    long Quantity,
    int FlagId,
    bool IsSingleton,
    bool IsBlueprintCopy,
    string ItemName);