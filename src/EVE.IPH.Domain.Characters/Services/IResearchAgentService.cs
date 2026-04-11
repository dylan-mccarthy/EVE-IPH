using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Characters.Services;

/// <summary>
/// Loads and refreshes a character's current research agents.
/// </summary>
public interface IResearchAgentService
{
    Task<Result<IReadOnlyList<ResearchAgent>>> GetAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ResearchAgent>>> RefreshAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}