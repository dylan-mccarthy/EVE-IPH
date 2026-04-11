using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Models;

namespace EVE.IPH.Infrastructure.ESI.Interfaces;

/// <summary>
/// Typed client for a focused subset of ESI endpoints needed by the initial character and market phases.
/// </summary>
public interface IEsiClient
{
    Task<Result<EsiCharacterProfile>> GetCharacterProfileAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EsiAsset>>> GetCharacterAssetsAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EsiAsset>>> GetCorporationAssetsAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EsiSkill>>> GetSkillsAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EsiStanding>>> GetStandingsAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EsiResearchAgent>>> GetResearchAgentsAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EsiIndustryJob>>> GetCharacterIndustryJobsAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EsiIndustryJob>>> GetCorporationIndustryJobsAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EsiEntityName>>> GetNamesAsync(
        IReadOnlyList<long> ids,
        CancellationToken cancellationToken = default);
}