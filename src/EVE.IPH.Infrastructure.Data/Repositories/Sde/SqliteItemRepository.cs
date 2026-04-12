using Dapper;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Connections;

namespace EVE.IPH.Infrastructure.Data.Repositories.Sde;

/// <summary>SQLite-backed implementation of <see cref="IItemRepository"/> reading from the EVE SDE.</summary>
public sealed class SqliteItemRepository : IItemRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteItemRepository(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<Maybe<ItemRecord>> GetItemAsync(TypeId typeId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT IT.typeID, IT.typeName, IT.groupID, IC.groupName, IC.categoryID, IT.volume, IT.portionSize
                FROM INVENTORY_TYPES AS IT
                INNER JOIN INVENTORY_CATEGORIES AS IC ON IT.groupID = IC.groupID
                WHERE IT.typeID = @TypeId
                """;

            ItemDto? row = await connection.QueryFirstOrDefaultAsync<ItemDto>(
                new CommandDefinition(sql, new { TypeId = typeId.Value }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return row is null ? Maybe<ItemRecord>.None : Maybe<ItemRecord>.Some(MapItem(row));
        }
        catch (Exception)
        {
            return Maybe<ItemRecord>.None;
        }
    }

    public async Task<Maybe<string>> GetItemNameAsync(TypeId typeId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT typeName FROM INVENTORY_TYPES WHERE typeID = @TypeId";

            string? name = await connection.QueryFirstOrDefaultAsync<string>(
                new CommandDefinition(sql, new { TypeId = typeId.Value }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return name is null ? Maybe<string>.None : Maybe<string>.Some(name);
        }
        catch (Exception)
        {
            return Maybe<string>.None;
        }
    }

    public async Task<Result<IReadOnlyList<ItemRecord>>> GetItemsByGroupAsync(int groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT IT.typeID, IT.typeName, IT.groupID, IC.groupName, IC.categoryID, IT.volume, IT.portionSize
                FROM INVENTORY_TYPES AS IT
                INNER JOIN INVENTORY_CATEGORIES AS IC ON IT.groupID = IC.groupID
                WHERE IT.groupID = @GroupId
                """;

            IEnumerable<ItemDto> rows = await connection.QueryAsync<ItemDto>(
                new CommandDefinition(sql, new { GroupId = groupId }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            IReadOnlyList<ItemRecord> items = rows.Select(MapItem).ToList();
            return Result<IReadOnlyList<ItemRecord>>.Success(items);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<ItemRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<ItemRecord>>> GetItemsByGroupNamesAsync(IReadOnlyCollection<string> groupNames, CancellationToken cancellationToken = default)
    {
        if (groupNames.Count == 0)
        {
            return Result<IReadOnlyList<ItemRecord>>.Success(Array.Empty<ItemRecord>());
        }

        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT DISTINCT IT.typeID, IT.typeName, IT.groupID, IC.groupName, IC.categoryID, IT.volume, IT.portionSize
                FROM INVENTORY_TYPES AS IT
                INNER JOIN INVENTORY_CATEGORIES AS IC ON IT.groupID = IC.groupID
                INNER JOIN ITEM_LOOKUP AS IL ON IL.typeID = IT.typeID
                WHERE IL.groupName IN @GroupNames
                ORDER BY IT.typeName, IT.typeID
                """;

            IEnumerable<ItemDto> rows = await connection.QueryAsync<ItemDto>(
                new CommandDefinition(sql, new { GroupNames = groupNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return Result<IReadOnlyList<ItemRecord>>.Success(rows.Select(MapItem).ToList());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<ItemRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<ItemRecord>>> GetItemsByCategoryPrefixAsync(string categoryNamePrefix, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryNamePrefix))
        {
            return Result<IReadOnlyList<ItemRecord>>.Failure("INVALID_CATEGORY_PREFIX", "Category prefix must not be empty.");
        }

        try
        {
            using System.Data.IDbConnection connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT DISTINCT IT.typeID, IT.typeName, IT.groupID, IC.groupName, IC.categoryID, IT.volume, IT.portionSize
                FROM INVENTORY_TYPES AS IT
                INNER JOIN INVENTORY_CATEGORIES AS IC ON IT.groupID = IC.groupID
                INNER JOIN ITEM_LOOKUP AS IL ON IL.typeID = IT.typeID
                WHERE IL.categoryName LIKE @CategoryPattern
                ORDER BY IT.typeName, IT.typeID
                """;

            IEnumerable<ItemDto> rows = await connection.QueryAsync<ItemDto>(
                new CommandDefinition(sql, new { CategoryPattern = categoryNamePrefix.Trim() + "%" }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return Result<IReadOnlyList<ItemRecord>>.Success(rows.Select(MapItem).ToList());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<ItemRecord>>.Failure("DB_ERROR", ex.Message);
        }
    }

    private static ItemRecord MapItem(ItemDto row) => new(
        new TypeId(row.typeID),
        row.typeName,
        row.groupID,
        row.groupName,
        row.categoryID,
        row.volume,
        row.portionSize);

    private sealed class ItemDto
    {
        public long typeID { get; init; }
        public string typeName { get; init; } = string.Empty;
        public int groupID { get; init; }
        public string groupName { get; init; } = string.Empty;
        public int categoryID { get; init; }
        public double volume { get; init; }
        public int portionSize { get; init; }
    }
}
