using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Fetches current character profile, skills, and standings from an external source.
/// </summary>
public interface ICharacterDataSource
{
    Task<Result<CharacterProfileData>> GetCharacterProfileAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CharacterSkillData>>> GetSkillsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CharacterStandingData>>> GetStandingsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}

/// <summary>Current character profile returned by an external source.</summary>
public sealed record CharacterProfileData(
    CharacterId CharacterId,
    string Name,
    CorporationId CorporationId,
    Maybe<AllianceId> AllianceId);

/// <summary>Current character skill returned by an external source.</summary>
public sealed record CharacterSkillData(
    TypeId SkillTypeId,
    string Name,
    int TrainedLevel,
    int ActiveLevel,
    long SkillPoints);

/// <summary>Current standing returned by an external source.</summary>
public sealed record CharacterStandingData(
    long NpcId,
    string NpcType,
    string NpcName,
    double Standing);