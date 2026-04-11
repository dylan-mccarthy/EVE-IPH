namespace EVE.IPH.Domain.Assets.Models;

public sealed record AssetHierarchyItem(
    long ItemId,
    long LocationId,
    long TypeId,
    string TypeCategory,
    AssetBlueprintKind BlueprintKind);