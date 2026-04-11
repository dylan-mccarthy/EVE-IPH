namespace EVE.IPH.Infrastructure.Data.Repositories.App;

public interface IAssetReadRepository
{
    Task<EVE.IPH.Domain.Core.Results.Result<IReadOnlyList<AssetScreenRecord>>> GetHydratedAssetsAsync(CancellationToken cancellationToken = default);
}

public sealed record AssetScreenRecord(
    long OwnerId,
    long ItemId,
    long LocationId,
    long TypeId,
    long Quantity,
    int FlagId,
    bool IsSingleton,
    bool IsBlueprintCopy,
    string ItemName,
    string TypeName,
    string GroupName,
    string CategoryName,
    string LocationName,
    string FlagText,
    bool Container,
    int SortOrder);