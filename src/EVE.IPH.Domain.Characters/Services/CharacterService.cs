using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Characters.Services;

/// <summary>
/// Coordinates repository-backed character loading with external refreshes.
/// </summary>
public sealed class CharacterService(
    ICharacterRepository characterRepository,
    ICharacterSkillRepository skillRepository,
    ICharacterStandingRepository standingRepository,
    ICharacterDataSource characterDataSource) : ICharacterService
{
    private readonly ICharacterRepository _characterRepository = characterRepository ?? throw new ArgumentNullException(nameof(characterRepository));
    private readonly ICharacterSkillRepository _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
    private readonly ICharacterStandingRepository _standingRepository = standingRepository ?? throw new ArgumentNullException(nameof(standingRepository));
    private readonly ICharacterDataSource _characterDataSource = characterDataSource ?? throw new ArgumentNullException(nameof(characterDataSource));

    public async Task<Result<CharacterSnapshot>> GetAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        Maybe<CharacterRecord> character = await _characterRepository.GetByIdAsync(characterId, cancellationToken).ConfigureAwait(false);
        if (character.HasNoValue)
        {
            return Result<CharacterSnapshot>.Failure("CHARACTER_NOT_FOUND", $"Character {characterId} was not found.");
        }

        Result<IReadOnlyList<CharacterSkillRecord>> skills = await _skillRepository.GetByCharacterIdAsync(characterId, cancellationToken).ConfigureAwait(false);
        if (skills.IsFailure)
        {
            return Result<CharacterSnapshot>.Failure(skills.Error);
        }

        Result<IReadOnlyList<CharacterStandingRecord>> standings = await _standingRepository.GetByCharacterIdAsync(characterId, cancellationToken).ConfigureAwait(false);
        if (standings.IsFailure)
        {
            return Result<CharacterSnapshot>.Failure(standings.Error);
        }

        return Result<CharacterSnapshot>.Success(new CharacterSnapshot(
            character.Value,
            skills.Value.Select(MapSkill).ToList(),
            standings.Value.Select(MapStanding).ToList()));
    }

    public async Task<Result<CharacterSnapshot>> RefreshAsync(
        CharacterId characterId,
        bool isDefault,
        CancellationToken cancellationToken = default)
    {
        Result<CharacterProfileData> profile = await _characterDataSource.GetCharacterProfileAsync(characterId, cancellationToken).ConfigureAwait(false);
        if (profile.IsFailure)
        {
            return Result<CharacterSnapshot>.Failure(profile.Error);
        }

        Result<IReadOnlyList<CharacterSkillData>> skills = await _characterDataSource.GetSkillsAsync(characterId, cancellationToken).ConfigureAwait(false);
        if (skills.IsFailure)
        {
            return Result<CharacterSnapshot>.Failure(skills.Error);
        }

        Result<IReadOnlyList<CharacterStandingData>> standings = await _characterDataSource.GetStandingsAsync(characterId, cancellationToken).ConfigureAwait(false);
        if (standings.IsFailure)
        {
            return Result<CharacterSnapshot>.Failure(standings.Error);
        }

        CharacterRecord characterRecord = new(
            profile.Value.CharacterId,
            profile.Value.Name,
            profile.Value.CorporationId,
            profile.Value.AllianceId,
            isDefault);

        Result<CharacterRecord> upsertCharacter = await _characterRepository.UpsertAsync(characterRecord, cancellationToken).ConfigureAwait(false);
        if (upsertCharacter.IsFailure)
        {
            return Result<CharacterSnapshot>.Failure(upsertCharacter.Error);
        }

        Result<IReadOnlyList<CharacterSkillRecord>> storedSkills = await _skillRepository.ReplaceAsync(
            characterId,
            skills.Value.Select(skill => new CharacterSkillRecord(
                characterId,
                skill.SkillTypeId,
                skill.Name,
                skill.TrainedLevel,
                skill.ActiveLevel,
                skill.SkillPoints,
                false,
                0)).ToList(),
            cancellationToken).ConfigureAwait(false);
        if (storedSkills.IsFailure)
        {
            return Result<CharacterSnapshot>.Failure(storedSkills.Error);
        }

        Result<IReadOnlyList<CharacterStandingRecord>> storedStandings = await _standingRepository.ReplaceAsync(
            characterId,
            standings.Value.Select(standing => new CharacterStandingRecord(
                characterId,
                standing.NpcId,
                standing.NpcType,
                standing.NpcName,
                standing.Standing)).ToList(),
            cancellationToken).ConfigureAwait(false);
        if (storedStandings.IsFailure)
        {
            return Result<CharacterSnapshot>.Failure(storedStandings.Error);
        }

        return Result<CharacterSnapshot>.Success(new CharacterSnapshot(
            upsertCharacter.Value,
            storedSkills.Value.Select(MapSkill).ToList(),
            storedStandings.Value.Select(MapStanding).ToList()));
    }

    private static Skill MapSkill(CharacterSkillRecord skill) => new(
        skill.SkillTypeId,
        skill.Name,
        skill.TrainedLevel,
        skill.ActiveLevel,
        skill.SkillPoints,
        skill.IsOverridden,
        skill.OverrideLevel);

    private static NpcStanding MapStanding(CharacterStandingRecord standing) => new(
        standing.NpcId,
        standing.NpcType,
        standing.NpcName,
        standing.Standing);
}