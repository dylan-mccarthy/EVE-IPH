using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Persists and retrieves character skill records from the application database.
/// </summary>
public interface ICharacterSkillRepository
{
    Task<Result<IReadOnlyList<CharacterSkillRecord>>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CharacterSkillRecord>>> ReplaceAsync(
        CharacterId characterId,
        IReadOnlyList<CharacterSkillRecord> skills,
        CancellationToken cancellationToken = default);
}

/// <summary>A stored character skill row.</summary>
public sealed record CharacterSkillRecord(
    CharacterId CharacterId,
    TypeId SkillTypeId,
    string Name,
    int TrainedLevel,
    int ActiveLevel,
    long SkillPoints,
    bool IsOverridden,
    int OverrideLevel);