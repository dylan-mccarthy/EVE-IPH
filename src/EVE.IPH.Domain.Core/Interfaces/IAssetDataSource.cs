using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Fetches current character-owned assets from an external source.
/// </summary>
public interface IAssetDataSource
{
    Task<Result<IReadOnlyList<AssetData>>> GetCharacterAssetsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AssetData>>> GetCorporationAssetsAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        CancellationToken cancellationToken = default);
}

/// <summary>Current asset data returned by an external source.</summary>
public sealed record AssetData(
    long OwnerId,
    long ItemId,
    long LocationId,
    TypeId TypeId,
    long Quantity,
    int FlagId,
    bool IsSingleton,
    bool IsBlueprintCopy,
    string ItemName);