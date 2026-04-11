using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

/// <summary>SQLite-backed implementation of <see cref="ICharacterSkillRepository"/>.</summary>
public sealed class SqliteCharacterSkillRepository : ICharacterSkillRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteCharacterSkillRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<IReadOnlyList<CharacterSkillRecord>>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            IEnumerable<CharacterSkillDto> rows = await connection.QueryAsync<CharacterSkillDto>(
                new CommandDefinition(
                    """
                    SELECT CHARACTER_ID, SKILL_TYPE_ID, SKILL_NAME, TRAINED_SKILL_LEVEL, ACTIVE_SKILL_LEVEL, SKILL_POINTS, OVERRIDE_SKILL, OVERRIDE_LEVEL
                    FROM CHARACTER_SKILLS
                    WHERE CHARACTER_ID = @CharacterId
                    ORDER BY SKILL_NAME, SKILL_TYPE_ID
                    """,
                    new { CharacterId = characterId.Value },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<IReadOnlyList<CharacterSkillRecord>>.Success(rows.Select(MapSkill).ToList());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CharacterSkillRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<CharacterSkillRecord>>> ReplaceAsync(
        CharacterId characterId,
        IReadOnlyList<CharacterSkillRecord> skills,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(skills);

        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            connection.Open();
            using System.Data.IDbTransaction transaction = connection.BeginTransaction();

            IReadOnlyList<CharacterSkillRecord> existingSkills = (await connection.QueryAsync<CharacterSkillDto>(
                new CommandDefinition(
                    """
                    SELECT CHARACTER_ID, SKILL_TYPE_ID, SKILL_NAME, TRAINED_SKILL_LEVEL, ACTIVE_SKILL_LEVEL, SKILL_POINTS, OVERRIDE_SKILL, OVERRIDE_LEVEL
                    FROM CHARACTER_SKILLS
                    WHERE CHARACTER_ID = @CharacterId
                    """,
                    new { CharacterId = characterId.Value },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false))
                .Select(MapSkill)
                .ToList();

            Dictionary<long, CharacterSkillRecord> existingBySkillId = existingSkills.ToDictionary(skill => skill.SkillTypeId.Value);
            HashSet<long> incomingSkillIds = skills.Select(skill => skill.SkillTypeId.Value).ToHashSet();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM CHARACTER_SKILLS WHERE CHARACTER_ID = @CharacterId",
                    new { CharacterId = characterId.Value },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            List<CharacterSkillRecord> mergedSkills = new(skills.Count + existingSkills.Count);

            foreach (CharacterSkillRecord skill in skills)
            {
                CharacterSkillRecord storedSkill = existingBySkillId.TryGetValue(skill.SkillTypeId.Value, out CharacterSkillRecord? existing)
                    ? skill with { IsOverridden = existing.IsOverridden, OverrideLevel = existing.OverrideLevel }
                    : skill;

                mergedSkills.Add(storedSkill);
            }

            foreach (CharacterSkillRecord existing in existingSkills)
            {
                if (!incomingSkillIds.Contains(existing.SkillTypeId.Value) && existing.IsOverridden)
                {
                    mergedSkills.Add(existing);
                }
            }

            foreach (CharacterSkillRecord skill in mergedSkills)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO CHARACTER_SKILLS (
                            CHARACTER_ID,
                            SKILL_TYPE_ID,
                            SKILL_NAME,
                            SKILL_POINTS,
                            TRAINED_SKILL_LEVEL,
                            ACTIVE_SKILL_LEVEL,
                            OVERRIDE_SKILL,
                            OVERRIDE_LEVEL)
                        VALUES (
                            @CharacterId,
                            @SkillTypeId,
                            @SkillName,
                            @SkillPoints,
                            @TrainedLevel,
                            @ActiveLevel,
                            @IsOverridden,
                            @OverrideLevel)
                        """,
                        new
                        {
                            CharacterId = skill.CharacterId.Value,
                            SkillTypeId = skill.SkillTypeId.Value,
                            SkillName = skill.Name,
                            SkillPoints = skill.SkillPoints,
                            TrainedLevel = skill.TrainedLevel,
                            ActiveLevel = skill.ActiveLevel,
                            IsOverridden = skill.IsOverridden ? 1 : 0,
                            skill.OverrideLevel,
                        },
                        transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            transaction.Commit();
            return Result<IReadOnlyList<CharacterSkillRecord>>.Success(mergedSkills.OrderBy(skill => skill.Name).ThenBy(skill => skill.SkillTypeId.Value).ToList());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CharacterSkillRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    private static CharacterSkillRecord MapSkill(CharacterSkillDto row) => new(
        new CharacterId(row.CHARACTER_ID),
        new TypeId(row.SKILL_TYPE_ID),
        row.SKILL_NAME,
        row.TRAINED_SKILL_LEVEL,
        row.ACTIVE_SKILL_LEVEL,
        row.SKILL_POINTS,
        row.OVERRIDE_SKILL != 0,
        row.OVERRIDE_LEVEL);

    private sealed class CharacterSkillDto
    {
        public long CHARACTER_ID { get; init; }
        public long SKILL_TYPE_ID { get; init; }
        public string SKILL_NAME { get; init; } = string.Empty;
        public int TRAINED_SKILL_LEVEL { get; init; }
        public int ACTIVE_SKILL_LEVEL { get; init; }
        public long SKILL_POINTS { get; init; }
        public int OVERRIDE_SKILL { get; init; }
        public int OVERRIDE_LEVEL { get; init; }
    }
}