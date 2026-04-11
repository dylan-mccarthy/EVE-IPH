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
            const string sql = "SELECT CHARACTER_ID, CHARACTER_NAME, CORPORATION_ID, IS_DEFAULT FROM ESI_CHARACTER_DATA ORDER BY CHARACTER_NAME";

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
            const string sql = "SELECT CHARACTER_ID, CHARACTER_NAME, CORPORATION_ID, IS_DEFAULT FROM ESI_CHARACTER_DATA WHERE CHARACTER_ID = @CharacterId";

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
                INSERT INTO ESI_CHARACTER_DATA (CHARACTER_ID, CHARACTER_NAME, CORPORATION_ID, IS_DEFAULT)
                VALUES (@CharacterId, @CharacterName, @CorporationId, @IsDefault)
                ON CONFLICT(CHARACTER_ID) DO UPDATE SET
                    CHARACTER_NAME = excluded.CHARACTER_NAME,
                    CORPORATION_ID = excluded.CORPORATION_ID,
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
            const string sql = "DELETE FROM ESI_CHARACTER_DATA WHERE CHARACTER_ID = @CharacterId";

            int affected = await connection.ExecuteAsync(
                new CommandDefinition(sql, new { CharacterId = characterId.Value }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

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
        Maybe<AllianceId>.None,
        row.IS_DEFAULT == 1);

    private sealed class CharacterDto
    {
        public long CHARACTER_ID { get; init; }
        public string CHARACTER_NAME { get; init; } = string.Empty;
        public long CORPORATION_ID { get; init; }
        public int IS_DEFAULT { get; init; }
    }
}
