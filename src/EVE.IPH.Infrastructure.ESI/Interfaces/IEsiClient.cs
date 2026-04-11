using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Infrastructure.ESI.Interfaces;

/// <summary>
/// Typed client for a focused subset of ESI endpoints needed by the initial character and market phases.
/// </summary>
public interface IEsiClient
{
    Task<Result<EsiCharacterProfile>> GetCharacterProfileAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EsiSkill>>> GetSkillsAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EsiStanding>>> GetStandingsAsync(CharacterId characterId, CancellationToken cancellationToken = default);
}