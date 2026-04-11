using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

/// <summary>SQLite-backed implementation of <see cref="IOwnedBlueprintRepository"/>.</summary>
public sealed class SqliteOwnedBlueprintRepository : IOwnedBlueprintRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteOwnedBlueprintRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<IReadOnlyList<OwnedBlueprintRecord>>> GetByUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT USER_ID, ITEM_ID, LOCATION_ID, BLUEPRINT_ID, BLUEPRINT_NAME,
                       QUANTITY, ME, TE, RUNS, BP_TYPE, OWNED, SCANNED
                FROM OWNED_BLUEPRINTS
                WHERE USER_ID = @UserId
                ORDER BY BLUEPRINT_NAME
                """;

            IEnumerable<OwnedBlueprintDto> rows = await connection.QueryAsync<OwnedBlueprintDto>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            IReadOnlyList<OwnedBlueprintRecord> records = rows.Select(MapRecord).ToList();
            return Result<IReadOnlyList<OwnedBlueprintRecord>>.Success(records);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<OwnedBlueprintRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<OwnedBlueprintRecord>> UpsertAsync(OwnedBlueprintRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                INSERT INTO OWNED_BLUEPRINTS
                    (USER_ID, ITEM_ID, LOCATION_ID, BLUEPRINT_ID, BLUEPRINT_NAME, QUANTITY, ME, TE, RUNS, BP_TYPE, OWNED, SCANNED)
                VALUES
                    (@UserId, @ItemId, @LocationId, @BlueprintId, @BlueprintName, @Quantity, @Me, @Te, @Runs, @BpType, @Owned, @Scanned)
                ON CONFLICT(USER_ID, BLUEPRINT_ID) DO UPDATE SET
                    ITEM_ID = excluded.ITEM_ID,
                    LOCATION_ID = excluded.LOCATION_ID,
                    BLUEPRINT_NAME = excluded.BLUEPRINT_NAME,
                    QUANTITY = excluded.QUANTITY,
                    ME = excluded.ME,
                    TE = excluded.TE,
                    RUNS = excluded.RUNS,
                    BP_TYPE = excluded.BP_TYPE,
                    OWNED = excluded.OWNED,
                    SCANNED = excluded.SCANNED
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(sql, ToParam(record), cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return Result<OwnedBlueprintRecord>.Success(record);
        }
        catch (Exception ex)
        {
            return Result<OwnedBlueprintRecord>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<bool>> DeleteAsync(long userId, BlueprintId blueprintId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "DELETE FROM OWNED_BLUEPRINTS WHERE USER_ID = @UserId AND BLUEPRINT_ID = @BlueprintId";

            int affected = await connection.ExecuteAsync(
                new CommandDefinition(sql, new { UserId = userId, BlueprintId = blueprintId.Value }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return Result<bool>.Success(affected > 0);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("DB_ERROR", ex.Message);
        }
    }

    private static object ToParam(OwnedBlueprintRecord r) => new
    {
        UserId = r.UserId,
        ItemId = r.ItemId.Value,
        LocationId = r.LocationId,
        BlueprintId = r.BlueprintId.Value,
        BlueprintName = r.BlueprintName,
        Quantity = r.Quantity,
        Me = r.Me,
        Te = r.Te,
        Runs = r.Runs,
        BpType = r.BpType,
        Owned = r.Owned ? 1 : 0,
        Scanned = r.Scanned ? 1 : 0,
    };

    private static OwnedBlueprintRecord MapRecord(OwnedBlueprintDto row) => new(
        row.USER_ID,
        new ItemId(row.ITEM_ID),
        row.LOCATION_ID,
        new BlueprintId(row.BLUEPRINT_ID),
        row.BLUEPRINT_NAME,
        row.QUANTITY,
        row.ME,
        row.TE,
        row.RUNS,
        row.BP_TYPE,
        row.OWNED == 1,
        row.SCANNED == 1);

    private sealed class OwnedBlueprintDto
    {
        public long USER_ID { get; init; }
        public long ITEM_ID { get; init; }
        public long LOCATION_ID { get; init; }
        public long BLUEPRINT_ID { get; init; }
        public string BLUEPRINT_NAME { get; init; } = string.Empty;
        public int QUANTITY { get; init; }
        public int ME { get; init; }
        public int TE { get; init; }
        public int RUNS { get; init; }
        public int BP_TYPE { get; init; }
        public int OWNED { get; init; }
        public int SCANNED { get; init; }
    }
}
