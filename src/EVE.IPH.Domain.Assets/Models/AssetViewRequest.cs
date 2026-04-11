namespace EVE.IPH.Domain.Assets.Models;

public sealed record AssetViewRequest(
    IReadOnlySet<long> OwnerIds,
    IReadOnlySet<long> TypeIds,
    string SearchText,
    bool OnlyBlueprintCopies,
    AssetSortMode SortMode);