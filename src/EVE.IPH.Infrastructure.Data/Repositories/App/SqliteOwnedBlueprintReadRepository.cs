using Dapper;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

public sealed class SqliteOwnedBlueprintReadRepository : IOwnedBlueprintViewRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteOwnedBlueprintReadRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<IReadOnlyList<OwnedBlueprintViewRecord>>> GetByOwnersAsync(
        IReadOnlyList<long> ownerIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ownerIds);

        if (ownerIds.Count == 0)
        {
            return Result<IReadOnlyList<OwnedBlueprintViewRecord>>.Success([]);
        }

        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT OBP.USER_ID,
                       COALESCE(ECD.CHARACTER_NAME, ECC.CORPORATION_NAME, CAST(OBP.USER_ID AS TEXT)) AS OWNER_NAME,
                       CASE WHEN ECC.CORPORATION_ID IS NOT NULL THEN 1 ELSE 0 END AS IS_CORPORATION_OWNER,
                       OBP.ITEM_ID,
                       OBP.LOCATION_ID,
                       OBP.BLUEPRINT_ID,
                       OBP.BLUEPRINT_NAME,
                       OBP.QUANTITY,
                       OBP.ME,
                       OBP.TE,
                       OBP.RUNS,
                       OBP.BP_TYPE,
                       OBP.OWNED,
                       OBP.SCANNED
                FROM OWNED_BLUEPRINTS AS OBP
                LEFT JOIN ESI_CHARACTER_DATA AS ECD ON ECD.CHARACTER_ID = OBP.USER_ID
                LEFT JOIN ESI_CORPORATION_CONNECTIONS AS ECC ON ECC.CORPORATION_ID = OBP.USER_ID
                WHERE OBP.USER_ID IN @OwnerIds
                ORDER BY OWNER_NAME, BLUEPRINT_NAME
                """;

            IEnumerable<OwnedBlueprintScreenDto> rows = await connection.QueryAsync<OwnedBlueprintScreenDto>(
                new CommandDefinition(sql, new { OwnerIds = ownerIds.ToArray() }, cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<IReadOnlyList<OwnedBlueprintViewRecord>>.Success(rows.Select(MapRecord).ToArray());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<OwnedBlueprintViewRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    private static OwnedBlueprintViewRecord MapRecord(OwnedBlueprintScreenDto row) => new(
        row.USER_ID,
        row.OWNER_NAME,
        row.IS_CORPORATION_OWNER == 1,
        row.ITEM_ID,
        row.LOCATION_ID,
        row.BLUEPRINT_ID,
        row.BLUEPRINT_NAME,
        row.QUANTITY,
        row.ME,
        row.TE,
        row.RUNS,
        row.BP_TYPE,
        row.OWNED == 1,
        row.SCANNED == 1);

    private sealed class OwnedBlueprintScreenDto
    {
        public long USER_ID { get; init; }
        public string OWNER_NAME { get; init; } = string.Empty;
        public int IS_CORPORATION_OWNER { get; init; }
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