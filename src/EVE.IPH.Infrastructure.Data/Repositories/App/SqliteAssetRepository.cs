using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

public sealed class SqliteAssetRepository : IAssetRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteAssetRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<IReadOnlyList<StoredAssetRecord>>> GetByOwnerIdAsync(
        long ownerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT ID, ItemID, LocationID, TypeID, Quantity, Flag, IsSingleton, IsBPCopy, ItemName
                FROM ASSETS
                WHERE ID = @OwnerId
                ORDER BY ItemID
                """;

            IEnumerable<AssetDto> rows = await connection.QueryAsync<AssetDto>(
                new CommandDefinition(sql, new { OwnerId = ownerId }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return Result<IReadOnlyList<StoredAssetRecord>>.Success(rows.Select(MapRecord).ToArray());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<StoredAssetRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<StoredAssetRecord>>> ReplaceAsync(
        long ownerId,
        IReadOnlyList<StoredAssetRecord> assets,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assets);

        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            connection.Open();
            using System.Data.IDbTransaction transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM ASSETS WHERE ID = @OwnerId",
                    new { OwnerId = ownerId },
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            foreach (StoredAssetRecord asset in assets)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO ASSETS (ID, ItemID, LocationID, TypeID, Quantity, Flag, IsSingleton, IsBPCopy, ItemName)
                        VALUES (@OwnerId, @ItemId, @LocationId, @TypeId, @Quantity, @FlagId, @IsSingleton, @IsBlueprintCopy, @ItemName)
                        """,
                        new
                        {
                            OwnerId = asset.OwnerId,
                            asset.ItemId,
                            asset.LocationId,
                            TypeId = asset.TypeId.Value,
                            asset.Quantity,
                            asset.FlagId,
                            IsSingleton = asset.IsSingleton ? 1 : 0,
                            IsBlueprintCopy = asset.IsBlueprintCopy ? -1 : 0,
                            asset.ItemName,
                        },
                        transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            transaction.Commit();

            return await GetByOwnerIdAsync(ownerId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<StoredAssetRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<bool>> DeleteByOwnerIdAsync(
        long ownerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            int affected = await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM ASSETS WHERE ID = @OwnerId",
                    new { OwnerId = ownerId },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            return Result<bool>.Success(affected > 0);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("DB_ERROR", ex.Message);
        }
    }

    private static StoredAssetRecord MapRecord(AssetDto row) => new(
        row.ID,
        row.ItemID,
        row.LocationID,
        new TypeId(row.TypeID),
        row.Quantity,
        row.Flag,
        row.IsSingleton,
        row.IsBPCopy,
        row.ItemName ?? string.Empty);

    private sealed class AssetDto
    {
        public long ID { get; init; }
        public long ItemID { get; init; }
        public long LocationID { get; init; }
        public long TypeID { get; init; }
        public long Quantity { get; init; }
        public int Flag { get; init; }
        public bool IsSingleton { get; init; }
        public bool IsBPCopy { get; init; }
        public string? ItemName { get; init; }
    }
}