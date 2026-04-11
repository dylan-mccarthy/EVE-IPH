namespace EVE.IPH.Domain.Assets.Models;

public sealed record AssetTreeItem(
    long ItemId,
    long LocationId,
    string LocationName,
    int FlagId,
    int FlagSort,
    string FlagText,
    bool Container,
    bool IsSingleton,
    long Quantity,
    string TypeGroup,
    string TypeCategory,
    string ItemText);