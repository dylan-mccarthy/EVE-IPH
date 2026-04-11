using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

public interface ICorporationConnectionRepository
{
    Task<Result<IReadOnlyList<CorporationConnectionRecord>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Maybe<CorporationConnectionRecord>> GetByIdAsync(CorporationId corporationId, CancellationToken cancellationToken = default);

    Task<Result<CorporationConnectionRecord>> UpsertAsync(CorporationConnectionRecord connection, CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteAsync(CorporationId corporationId, CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteByAuthorizedCharacterIdAsync(CharacterId characterId, CancellationToken cancellationToken = default);
}

public sealed record CorporationConnectionRecord(
    CorporationId CorporationId,
    string Name,
    CharacterId AuthorizedCharacterId,
    bool HasAssetAccess,
    bool HasIndustryJobAccess,
    bool HasBlueprintAccess);