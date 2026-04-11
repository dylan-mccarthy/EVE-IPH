using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Persists and retrieves NPC standings for a character.
/// </summary>
public interface ICharacterStandingRepository
{
    Task<Result<IReadOnlyList<CharacterStandingRecord>>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CharacterStandingRecord>>> ReplaceAsync(
        CharacterId characterId,
        IReadOnlyList<CharacterStandingRecord> standings,
        CancellationToken cancellationToken = default);
}

/// <summary>A stored character standing row.</summary>
public sealed record CharacterStandingRecord(
    CharacterId CharacterId,
    long NpcId,
    string NpcType,
    string NpcName,
    double Standing);