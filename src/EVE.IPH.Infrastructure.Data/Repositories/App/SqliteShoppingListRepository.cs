using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

/// <summary>SQLite-backed implementation of <see cref="IShoppingListRepository"/>.</summary>
public sealed class SqliteShoppingListRepository : IShoppingListRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteShoppingListRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<IReadOnlyList<ShoppingListItemRecord>>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT TYPE_ID, ITEM_NAME, QUANTITY, PRICE FROM SHOPPING_LIST_ITEMS ORDER BY ITEM_NAME";

            IEnumerable<ShoppingListItemDto> rows = await connection.QueryAsync<ShoppingListItemDto>(
                new CommandDefinition(sql, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            IReadOnlyList<ShoppingListItemRecord> items = rows.Select(MapRecord).ToList();
            return Result<IReadOnlyList<ShoppingListItemRecord>>.Success(items);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<ShoppingListItemRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<ShoppingListItemRecord>> UpsertItemAsync(ShoppingListItemRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                INSERT INTO SHOPPING_LIST_ITEMS (TYPE_ID, ITEM_NAME, QUANTITY, PRICE)
                VALUES (@TypeId, @ItemName, @Quantity, @Price)
                ON CONFLICT(TYPE_ID) DO UPDATE SET
                    ITEM_NAME = excluded.ITEM_NAME,
                    QUANTITY = excluded.QUANTITY,
                    PRICE = excluded.PRICE
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { TypeId = record.TypeId.Value, record.ItemName, record.Quantity, record.Price },
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return Result<ShoppingListItemRecord>.Success(record);
        }
        catch (Exception ex)
        {
            return Result<ShoppingListItemRecord>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<bool>> DeleteItemAsync(TypeId typeId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "DELETE FROM SHOPPING_LIST_ITEMS WHERE TYPE_ID = @TypeId";

            int affected = await connection.ExecuteAsync(
                new CommandDefinition(sql, new { TypeId = typeId.Value }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return Result<bool>.Success(affected > 0);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<bool>> ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                new CommandDefinition("DELETE FROM SHOPPING_LIST_ITEMS", cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("DB_ERROR", ex.Message);
        }
    }

    private static ShoppingListItemRecord MapRecord(ShoppingListItemDto row) => new(
        new TypeId(row.TYPE_ID),
        row.ITEM_NAME,
        row.QUANTITY,
        row.PRICE);

    private sealed class ShoppingListItemDto
    {
        public long TYPE_ID { get; init; }
        public string ITEM_NAME { get; init; } = string.Empty;
        public long QUANTITY { get; init; }
        public double PRICE { get; init; }
    }
}
