using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

/// <summary>SQLite-backed implementation of <see cref="ICharacterStandingRepository"/>.</summary>
public sealed class SqliteCharacterStandingRepository : ICharacterStandingRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteCharacterStandingRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<IReadOnlyList<CharacterStandingRecord>>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            IEnumerable<CharacterStandingDto> rows = await connection.QueryAsync<CharacterStandingDto>(
                new CommandDefinition(
                    """
                    SELECT CHARACTER_ID, NPC_TYPE_ID, NPC_TYPE, NPC_NAME, STANDING
                    FROM CHARACTER_STANDINGS
                    WHERE CHARACTER_ID = @CharacterId
                    ORDER BY NPC_TYPE, NPC_NAME, NPC_TYPE_ID
                    """,
                    new { CharacterId = characterId.Value },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<IReadOnlyList<CharacterStandingRecord>>.Success(rows.Select(MapStanding).ToList());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CharacterStandingRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<CharacterStandingRecord>>> ReplaceAsync(
        CharacterId characterId,
        IReadOnlyList<CharacterStandingRecord> standings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(standings);

        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            connection.Open();
            using System.Data.IDbTransaction transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM CHARACTER_STANDINGS WHERE CHARACTER_ID = @CharacterId",
                    new { CharacterId = characterId.Value },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            foreach (CharacterStandingRecord standing in standings)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO CHARACTER_STANDINGS (CHARACTER_ID, NPC_TYPE_ID, NPC_TYPE, NPC_NAME, STANDING)
                        VALUES (@CharacterId, @NpcId, @NpcType, @NpcName, @Standing)
                        """,
                        new
                        {
                            CharacterId = standing.CharacterId.Value,
                            standing.NpcId,
                            standing.NpcType,
                            standing.NpcName,
                            standing.Standing,
                        },
                        transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            transaction.Commit();
            return Result<IReadOnlyList<CharacterStandingRecord>>.Success(standings.OrderBy(standing => standing.NpcType).ThenBy(standing => standing.NpcName).ThenBy(standing => standing.NpcId).ToList());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CharacterStandingRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    private static CharacterStandingRecord MapStanding(CharacterStandingDto row) => new(
        new CharacterId(row.CHARACTER_ID),
        row.NPC_TYPE_ID,
        row.NPC_TYPE,
        row.NPC_NAME,
        row.STANDING);

    private sealed class CharacterStandingDto
    {
        public long CHARACTER_ID { get; init; }
        public long NPC_TYPE_ID { get; init; }
        public string NPC_TYPE { get; init; } = string.Empty;
        public string NPC_NAME { get; init; } = string.Empty;
        public double STANDING { get; init; }
    }
}