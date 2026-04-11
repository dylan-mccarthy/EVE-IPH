using Dapper;
using EVE.IPH.Domain.Core;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.UI.Avalonia.Startup;

/// <summary>
/// Ensures the local all-skills-V placeholder character exists so calculations always have a fallback character context.
/// </summary>
public sealed class DummyCharacterBootstrapper
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DummyCharacterBootstrapper(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task EnsureDummyCharacterAsync(CancellationToken cancellationToken = default)
    {
        SqliteCharacterRepository characterRepository = new(_connectionFactory);
        SqliteCharacterSkillRepository skillRepository = new(_connectionFactory);
        SqliteCharacterStandingRepository standingRepository = new(_connectionFactory);

        Result<IReadOnlyList<CharacterRecord>> charactersResult = await characterRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        if (charactersResult.IsFailure)
        {
            throw new InvalidOperationException(charactersResult.Error.Message);
        }

        bool hasRealCharacters = charactersResult.Value.Any(character => !SpecialCharacters.IsAllSkillsV(character.CharacterId));
        CharacterRecord dummyCharacter = new(
            SpecialCharacters.AllSkillsVId,
            SpecialCharacters.AllSkillsVName,
            SpecialCharacters.PlaceholderCorporationId,
            Maybe<AllianceId>.None,
            !hasRealCharacters);

        Result<CharacterRecord> upsertCharacter = await characterRepository.UpsertAsync(dummyCharacter, cancellationToken).ConfigureAwait(false);
        if (upsertCharacter.IsFailure)
        {
            throw new InvalidOperationException(upsertCharacter.Error.Message);
        }

        IReadOnlyList<CharacterSkillRecord> dummySkills = await LoadDummySkillsAsync(cancellationToken).ConfigureAwait(false);
        Result<IReadOnlyList<CharacterSkillRecord>> replaceSkills = await skillRepository
            .ReplaceAsync(SpecialCharacters.AllSkillsVId, dummySkills, cancellationToken)
            .ConfigureAwait(false);
        if (replaceSkills.IsFailure)
        {
            throw new InvalidOperationException(replaceSkills.Error.Message);
        }

        Result<IReadOnlyList<CharacterStandingRecord>> replaceStandings = await standingRepository
            .ReplaceAsync(SpecialCharacters.AllSkillsVId, [], cancellationToken)
            .ConfigureAwait(false);
        if (replaceStandings.IsFailure)
        {
            throw new InvalidOperationException(replaceStandings.Error.Message);
        }

        await ClearDummySkillOverridesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<CharacterSkillRecord>> LoadDummySkillsAsync(CancellationToken cancellationToken)
    {
        using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
        IEnumerable<SkillTypeRow> rows = await connection.QueryAsync<SkillTypeRow>(
            new CommandDefinition(
                "SELECT typeID, typeName FROM ITEM_LOOKUP WHERE categoryName = 'Skill' ORDER BY typeName, typeID",
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.Select(row => new CharacterSkillRecord(
            SpecialCharacters.AllSkillsVId,
            new TypeId(row.typeID),
            row.typeName,
            5,
            5,
            0,
            false,
            0)).ToList();
    }

    private async Task ClearDummySkillOverridesAsync(CancellationToken cancellationToken)
    {
        using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE CHARACTER_SKILLS SET OVERRIDE_SKILL = 0, OVERRIDE_LEVEL = 0 WHERE CHARACTER_ID = @CharacterId",
                new { CharacterId = SpecialCharacters.AllSkillsVId.Value },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private sealed class SkillTypeRow
    {
        public long typeID { get; init; }

        public string typeName { get; init; } = string.Empty;
    }
}