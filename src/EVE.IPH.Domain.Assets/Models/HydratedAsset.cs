using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Assets.Models;

public sealed record HydratedAsset(
    long OwnerId,
    long ItemId,
    long LocationId,
    TypeId TypeId,
    long Quantity,
    int FlagId,
    bool IsSingleton,
    AssetBlueprintKind BlueprintKind,
    string ItemName,
    string TypeName,
    string TypeGroup,
    string TypeCategory,
    string LocationName,
    string FlagText,
    bool Container,
    int FlagSort);