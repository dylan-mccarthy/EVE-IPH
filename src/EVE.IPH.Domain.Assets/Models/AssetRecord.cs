using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Assets.Models;

public sealed record AssetRecord(
    long OwnerId,
    long ItemId,
    long LocationId,
    TypeId TypeId,
    long Quantity,
    int FlagId,
    bool IsSingleton,
    bool IsBlueprintCopy,
    string ItemName);