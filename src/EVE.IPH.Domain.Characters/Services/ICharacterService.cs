using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Characters.Services;

/// <summary>
/// Orchestrates loading and refreshing character state through repositories and external sources.
/// </summary>
public interface ICharacterService
{
    Task<Result<CharacterSnapshot>> GetAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<CharacterSnapshot>> RefreshAsync(
        CharacterId characterId,
        bool isDefault,
        CancellationToken cancellationToken = default);
}