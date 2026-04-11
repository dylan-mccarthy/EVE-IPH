namespace EVE.IPH.Domain.Assets.Models;

public sealed record AssetDisplayItem(
    string TypeName,
    string TypeCategory,
    AssetBlueprintKind BlueprintKind,
    long Quantity,
    int FlagId,
    bool IsSingleton);