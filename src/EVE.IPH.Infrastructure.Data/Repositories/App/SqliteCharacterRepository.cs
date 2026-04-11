using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

/// <summary>SQLite-backed implementation of <see cref="ICharacterRepository"/>.</summary>
public sealed class SqliteCharacterRepository : ICharacterRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteCharacterRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<IReadOnlyList<CharacterRecord>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT CHARACTER_ID, CHARACTER_NAME, CORPORATION_ID, ALLIANCE_ID, IS_DEFAULT FROM ESI_CHARACTER_DATA ORDER BY CHARACTER_NAME";

            IEnumerable<CharacterDto> rows = await connection.QueryAsync<CharacterDto>(
                new CommandDefinition(sql, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            IReadOnlyList<CharacterRecord> records = rows.Select(MapCharacter).ToList();
            return Result<IReadOnlyList<CharacterRecord>>.Success(records);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CharacterRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Maybe<CharacterRecord>> GetByIdAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT CHARACTER_ID, CHARACTER_NAME, CORPORATION_ID, ALLIANCE_ID, IS_DEFAULT FROM ESI_CHARACTER_DATA WHERE CHARACTER_ID = @CharacterId";

            CharacterDto? row = await connection.QueryFirstOrDefaultAsync<CharacterDto>(
                new CommandDefinition(sql, new { CharacterId = characterId.Value }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return row is null ? Maybe<CharacterRecord>.None : Maybe<CharacterRecord>.Some(MapCharacter(row));
        }
        catch (Exception)
        {
            return Maybe<CharacterRecord>.None;
        }
    }

    public async Task<Result<CharacterRecord>> UpsertAsync(CharacterRecord character, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                INSERT INTO ESI_CHARACTER_DATA (CHARACTER_ID, CHARACTER_NAME, CORPORATION_ID, ALLIANCE_ID, IS_DEFAULT)
                VALUES (@CharacterId, @CharacterName, @CorporationId, @AllianceId, @IsDefault)
                ON CONFLICT(CHARACTER_ID) DO UPDATE SET
                    CHARACTER_NAME = excluded.CHARACTER_NAME,
                    CORPORATION_ID = excluded.CORPORATION_ID,
                    ALLIANCE_ID = excluded.ALLIANCE_ID,
                    IS_DEFAULT = excluded.IS_DEFAULT
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        CharacterId = character.CharacterId.Value,
                        CharacterName = character.Name,
                        CorporationId = character.CorporationId.Value,
                        AllianceId = character.AllianceId.HasValue ? (long?)character.AllianceId.Value.Value : null,
                        IsDefault = character.IsDefault ? 1 : 0,
                    },
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return Result<CharacterRecord>.Success(character);
        }
        catch (Exception ex)
        {
            return Result<CharacterRecord>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<bool>> DeleteAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            connection.Open();
            using System.Data.IDbTransaction transaction = connection.BeginTransaction();

            int affected = await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM CURRENT_RESEARCH_AGENTS WHERE CHARACTER_ID = @CharacterId",
                    new { CharacterId = characterId.Value },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM CHARACTER_STANDINGS WHERE CHARACTER_ID = @CharacterId",
                    new { CharacterId = characterId.Value },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM CHARACTER_SKILLS WHERE CHARACTER_ID = @CharacterId",
                    new { CharacterId = characterId.Value },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM ESI_CORPORATION_CONNECTIONS WHERE AUTHORIZED_CHARACTER_ID = @CharacterId",
                    new { CharacterId = characterId.Value },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            affected = await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM ESI_CHARACTER_DATA WHERE CHARACTER_ID = @CharacterId",
                    new { CharacterId = characterId.Value },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            transaction.Commit();

            return Result<bool>.Success(affected > 0);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("DB_ERROR", ex.Message);
        }
    }

    private static CharacterRecord MapCharacter(CharacterDto row) => new(
        new CharacterId(row.CHARACTER_ID),
        row.CHARACTER_NAME,
        new CorporationId(row.CORPORATION_ID),
        row.ALLIANCE_ID.HasValue ? Maybe<AllianceId>.Some(new AllianceId(row.ALLIANCE_ID.Value)) : Maybe<AllianceId>.None,
        row.IS_DEFAULT == 1);

    private sealed class CharacterDto
    {
        public long CHARACTER_ID { get; init; }
        public string CHARACTER_NAME { get; init; } = string.Empty;
        public long CORPORATION_ID { get; init; }
        public long? ALLIANCE_ID { get; init; }
        public int IS_DEFAULT { get; init; }
    }
}
