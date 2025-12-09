namespace server.Services.Assets;

public interface IAssetsService
{
    Task<List<Asset>> GetAssetsAsync(long characterId, CancellationToken ct = default);
}

public sealed record Asset(
    long ItemId,
    long LocationId,
    string LocationFlag,
    string LocationType,
    int TypeId,
    int Quantity,
    bool IsSingleton,
    bool? IsBlueprintCopy
);
