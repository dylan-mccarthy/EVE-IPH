using System.Globalization;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Interfaces;
using EVE.IPH.Infrastructure.ESI.Models;

namespace EVE.IPH.Infrastructure.ESI;

/// <summary>
/// Adapts ESI transport models into character-domain source records.
/// </summary>
public sealed class EsiCharacterDataSource(
    IEsiClient esiClient,
    IItemRepository itemRepository) : ICharacterDataSource
{
    private readonly IEsiClient _esiClient = esiClient ?? throw new ArgumentNullException(nameof(esiClient));
    private readonly IItemRepository _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));

    public async Task<Result<CharacterProfileData>> GetCharacterProfileAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        Result<EsiCharacterProfile> profile = await _esiClient.GetCharacterProfileAsync(characterId, cancellationToken).ConfigureAwait(false);
        if (profile.IsFailure)
        {
            return Result<CharacterProfileData>.Failure(profile.Error);
        }

        return Result<CharacterProfileData>.Success(new CharacterProfileData(
            profile.Value.CharacterId,
            profile.Value.Name,
            profile.Value.CorporationId,
            profile.Value.AllianceId));
    }

    public async Task<Result<IReadOnlyList<CharacterSkillData>>> GetSkillsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<EsiSkill>> skills = await _esiClient.GetSkillsAsync(characterId, cancellationToken).ConfigureAwait(false);
        if (skills.IsFailure)
        {
            return Result<IReadOnlyList<CharacterSkillData>>.Failure(skills.Error);
        }

        List<CharacterSkillData> mappedSkills = new(skills.Value.Count);
        foreach (EsiSkill skill in skills.Value)
        {
            Maybe<string> itemName = await _itemRepository.GetItemNameAsync(skill.SkillTypeId, cancellationToken).ConfigureAwait(false);
            string skillName = itemName.HasValue
                ? itemName.Value
                : skill.SkillTypeId.Value.ToString(CultureInfo.InvariantCulture);

            mappedSkills.Add(new CharacterSkillData(
                skill.SkillTypeId,
                skillName,
                skill.TrainedSkillLevel,
                skill.ActiveSkillLevel,
                skill.SkillPointsInSkill));
        }

        return Result<IReadOnlyList<CharacterSkillData>>.Success(mappedSkills);
    }

    public async Task<Result<IReadOnlyList<CharacterStandingData>>> GetStandingsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<EsiStanding>> standings = await _esiClient.GetStandingsAsync(characterId, cancellationToken).ConfigureAwait(false);
        if (standings.IsFailure)
        {
            return Result<IReadOnlyList<CharacterStandingData>>.Failure(standings.Error);
        }

        IReadOnlyList<long> standingIds = standings.Value.Select(standing => standing.FromId).Distinct().ToList();
        Result<IReadOnlyList<EsiEntityName>> names = await _esiClient.GetNamesAsync(standingIds, cancellationToken).ConfigureAwait(false);
        if (names.IsFailure)
        {
            return Result<IReadOnlyList<CharacterStandingData>>.Failure(names.Error);
        }

        Dictionary<long, string> namesById = names.Value.ToDictionary(entry => entry.Id, entry => entry.Name);
        IReadOnlyList<CharacterStandingData> mappedStandings = standings.Value
            .Select(standing => new CharacterStandingData(
                standing.FromId,
                MapStandingType(standing.FromType),
                namesById.GetValueOrDefault(standing.FromId, string.Empty),
                standing.Standing))
            .ToList();

        return Result<IReadOnlyList<CharacterStandingData>>.Success(mappedStandings);
    }

    private static string MapStandingType(string fromType) => fromType switch
    {
        "agents" => "Agent",
        "faction" => "Faction",
        "npc_corp" => "Corporation",
        _ => fromType,
    };
}