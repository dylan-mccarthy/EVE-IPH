using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

public sealed class SqliteCorporationConnectionRepository : ICorporationConnectionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteCorporationConnectionRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<IReadOnlyList<CorporationConnectionRecord>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT CORPORATION_ID, CORPORATION_NAME, AUTHORIZED_CHARACTER_ID, HAS_ASSET_ACCESS, HAS_INDUSTRY_JOB_ACCESS, HAS_BLUEPRINT_ACCESS FROM ESI_CORPORATION_CONNECTIONS ORDER BY CORPORATION_NAME";

            IEnumerable<CorporationConnectionDto> rows = await connection.QueryAsync<CorporationConnectionDto>(
                new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<IReadOnlyList<CorporationConnectionRecord>>.Success(rows.Select(MapRecord).ToArray());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CorporationConnectionRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Maybe<CorporationConnectionRecord>> GetByIdAsync(CorporationId corporationId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT CORPORATION_ID, CORPORATION_NAME, AUTHORIZED_CHARACTER_ID, HAS_ASSET_ACCESS, HAS_INDUSTRY_JOB_ACCESS, HAS_BLUEPRINT_ACCESS FROM ESI_CORPORATION_CONNECTIONS WHERE CORPORATION_ID = @CorporationId";

            CorporationConnectionDto? row = await connection.QueryFirstOrDefaultAsync<CorporationConnectionDto>(
                new CommandDefinition(sql, new { CorporationId = corporationId.Value }, cancellationToken: cancellationToken)).ConfigureAwait(false);

            return row is null ? Maybe<CorporationConnectionRecord>.None : Maybe<CorporationConnectionRecord>.Some(MapRecord(row));
        }
        catch (Exception)
        {
            return Maybe<CorporationConnectionRecord>.None;
        }
    }

    public async Task<Result<CorporationConnectionRecord>> UpsertAsync(CorporationConnectionRecord connection, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection dbConnection = _connectionFactory.CreateConnection();
            const string sql = """
                INSERT INTO ESI_CORPORATION_CONNECTIONS (CORPORATION_ID, CORPORATION_NAME, AUTHORIZED_CHARACTER_ID, HAS_ASSET_ACCESS, HAS_INDUSTRY_JOB_ACCESS, HAS_BLUEPRINT_ACCESS)
                VALUES (@CorporationId, @CorporationName, @AuthorizedCharacterId, @HasAssetAccess, @HasIndustryJobAccess, @HasBlueprintAccess)
                ON CONFLICT(CORPORATION_ID) DO UPDATE SET
                    CORPORATION_NAME = excluded.CORPORATION_NAME,
                    AUTHORIZED_CHARACTER_ID = excluded.AUTHORIZED_CHARACTER_ID,
                    HAS_ASSET_ACCESS = excluded.HAS_ASSET_ACCESS,
                    HAS_INDUSTRY_JOB_ACCESS = excluded.HAS_INDUSTRY_JOB_ACCESS,
                    HAS_BLUEPRINT_ACCESS = excluded.HAS_BLUEPRINT_ACCESS
                """;

            await dbConnection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        CorporationId = connection.CorporationId.Value,
                        CorporationName = connection.Name,
                        AuthorizedCharacterId = connection.AuthorizedCharacterId.Value,
                        HasAssetAccess = connection.HasAssetAccess ? 1 : 0,
                        HasIndustryJobAccess = connection.HasIndustryJobAccess ? 1 : 0,
                        HasBlueprintAccess = connection.HasBlueprintAccess ? 1 : 0,
                    },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<CorporationConnectionRecord>.Success(connection);
        }
        catch (Exception ex)
        {
            return Result<CorporationConnectionRecord>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<bool>> DeleteAsync(CorporationId corporationId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            int affected = await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM ESI_CORPORATION_CONNECTIONS WHERE CORPORATION_ID = @CorporationId",
                    new { CorporationId = corporationId.Value },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<bool>.Success(affected > 0);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<bool>> DeleteByAuthorizedCharacterIdAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            int affected = await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM ESI_CORPORATION_CONNECTIONS WHERE AUTHORIZED_CHARACTER_ID = @CharacterId",
                    new { CharacterId = characterId.Value },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<bool>.Success(affected > 0);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("DB_ERROR", ex.Message);
        }
    }

    private static CorporationConnectionRecord MapRecord(CorporationConnectionDto row) => new(
        new CorporationId(row.CORPORATION_ID),
        row.CORPORATION_NAME,
        new CharacterId(row.AUTHORIZED_CHARACTER_ID),
        row.HAS_ASSET_ACCESS == 1,
        row.HAS_INDUSTRY_JOB_ACCESS == 1,
        row.HAS_BLUEPRINT_ACCESS == 1);

    private sealed class CorporationConnectionDto
    {
        public long CORPORATION_ID { get; init; }
        public string CORPORATION_NAME { get; init; } = string.Empty;
        public long AUTHORIZED_CHARACTER_ID { get; init; }
        public int HAS_ASSET_ACCESS { get; init; }
        public int HAS_INDUSTRY_JOB_ACCESS { get; init; }
        public int HAS_BLUEPRINT_ACCESS { get; init; }
    }
}