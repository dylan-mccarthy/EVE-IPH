using Microsoft.Data.Sqlite;
using server.Infrastructure;
using server.Models;

namespace server.Services.Blueprints;

public sealed class BlueprintService : IBlueprintService
{
    private readonly ISqliteConnectionFactory _connections;

    public BlueprintService(ISqliteConnectionFactory connections)
    {
        _connections = connections;
    }

    public async Task<BlueprintSearchResponse> SearchAsync(BlueprintSearchRequest request, CancellationToken ct = default)
    {
        var page = Math.Max(1, request.Page ?? 1);
        var size = Math.Clamp(request.PageSize ?? 20, 1, 200);
        var offset = (page - 1) * size;

        // Use ALL_BLUEPRINTS (legacy schema) and allow search on name/group/category.
        const string sqlCount = @"
            SELECT COUNT(1)
            FROM ALL_BLUEPRINTS AS bp
            WHERE (@q IS NULL OR bp.BLUEPRINT_NAME LIKE @qLike OR bp.ITEM_GROUP LIKE @qLike OR bp.ITEM_CATEGORY LIKE @qLike)
              AND (@group IS NULL OR bp.ITEM_GROUP = @group)
              AND (@cat IS NULL OR bp.ITEM_CATEGORY = @cat);";

        const string sqlPage = @"
            SELECT bp.BLUEPRINT_ID, bp.BLUEPRINT_NAME, bp.ITEM_GROUP, bp.ITEM_CATEGORY
            FROM ALL_BLUEPRINTS AS bp
            WHERE (@q IS NULL OR bp.BLUEPRINT_NAME LIKE @qLike OR bp.ITEM_GROUP LIKE @qLike OR bp.ITEM_CATEGORY LIKE @qLike)
              AND (@group IS NULL OR bp.ITEM_GROUP = @group)
              AND (@cat IS NULL OR bp.ITEM_CATEGORY = @cat)
            ORDER BY bp.BLUEPRINT_NAME
            LIMIT @limit OFFSET @offset;";

        await using var conn = _connections.Create();
        await conn.OpenAsync(ct);

        var qLike = request.Query is null ? null : $"%{request.Query}%";

        var totalCmd = conn.CreateCommand();
        totalCmd.CommandText = sqlCount;
        totalCmd.Parameters.AddWithValue("@q", (object?)request.Query ?? DBNull.Value);
        totalCmd.Parameters.AddWithValue("@qLike", (object?)qLike ?? DBNull.Value);
        totalCmd.Parameters.AddWithValue("@group", (object?)request.Group ?? DBNull.Value);
        totalCmd.Parameters.AddWithValue("@cat", (object?)request.Category ?? DBNull.Value);
        var total = Convert.ToInt32(await totalCmd.ExecuteScalarAsync(ct));

        var pageCmd = conn.CreateCommand();
        pageCmd.CommandText = sqlPage;
        pageCmd.Parameters.AddWithValue("@q", (object?)request.Query ?? DBNull.Value);
        pageCmd.Parameters.AddWithValue("@qLike", (object?)qLike ?? DBNull.Value);
        pageCmd.Parameters.AddWithValue("@group", (object?)request.Group ?? DBNull.Value);
        pageCmd.Parameters.AddWithValue("@cat", (object?)request.Category ?? DBNull.Value);
        pageCmd.Parameters.AddWithValue("@limit", size);
        pageCmd.Parameters.AddWithValue("@offset", offset);

        var items = new List<BlueprintSummary>();
        await using var reader = await pageCmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var id = reader.GetInt64(0);
            var name = reader.GetString(1);
            var group = reader.GetString(2);
            var cat = reader.GetString(3);
            items.Add(new BlueprintSummary(id, name, group, cat));
        }

        return new BlueprintSearchResponse(items, total, page, size);
    }
}
