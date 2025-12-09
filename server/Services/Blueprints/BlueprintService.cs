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

    public async Task<BlueprintDetails?> GetDetailsAsync(long blueprintId, CancellationToken ct = default)
    {
        await using var conn = _connections.Create();
        await conn.OpenAsync(ct);

        // Get blueprint basic info
        const string sqlBp = @"
            SELECT BLUEPRINT_ID, BLUEPRINT_NAME, ITEM_GROUP, ITEM_CATEGORY
            FROM ALL_BLUEPRINTS
            WHERE BLUEPRINT_ID = @id";

        var bpCmd = conn.CreateCommand();
        bpCmd.CommandText = sqlBp;
        bpCmd.Parameters.AddWithValue("@id", blueprintId);

        string? bpName = null, group = null, category = null;
        await using (var reader = await bpCmd.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct))
                return null;

            bpName = reader.GetString(1);
            group = reader.GetString(2);
            category = reader.GetString(3);
        }

        // Get all activities with materials
        const string sqlActivities = @"
            SELECT DISTINCT ACTIVITY
            FROM ALL_BLUEPRINT_MATERIALS_FACT
            WHERE BLUEPRINT_ID = @id
            ORDER BY ACTIVITY";

        var actCmd = conn.CreateCommand();
        actCmd.CommandText = sqlActivities;
        actCmd.Parameters.AddWithValue("@id", blueprintId);

        var activityIds = new List<int>();
        await using (var reader = await actCmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                activityIds.Add(reader.GetInt32(0));
            }
        }

        var activities = new List<BlueprintActivity>();
        foreach (var activityId in activityIds)
        {
            var activity = await GetActivityDetailsAsync(conn, blueprintId, activityId, ct);
            if (activity != null)
                activities.Add(activity);
        }

        return new BlueprintDetails(blueprintId, bpName!, group!, category!, activities);
    }

    private async Task<BlueprintActivity?> GetActivityDetailsAsync(
        SqliteConnection conn,
        long blueprintId,
        int activityId,
        CancellationToken ct)
    {
        // Get activity name
        var activityName = GetActivityName(activityId);

        // Get materials
        const string sqlMaterials = @"
            SELECT MATERIAL_ID, MATERIAL, MATERIAL_GROUP, MATERIAL_CATEGORY, 
                   QUANTITY, MATERIAL_VOLUME, CONSUME, PRODUCT_ID
            FROM ALL_BLUEPRINT_MATERIALS
            WHERE BLUEPRINT_ID = @blueprintId AND ACTIVITY = @activityId
            ORDER BY MATERIAL";

        var matCmd = conn.CreateCommand();
        matCmd.CommandText = sqlMaterials;
        matCmd.Parameters.AddWithValue("@blueprintId", blueprintId);
        matCmd.Parameters.AddWithValue("@activityId", activityId);

        var materials = new List<BlueprintMaterial>();
        long productId = 0;
        await using (var reader = await matCmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var matId = reader.GetInt64(0);
                var matName = reader.GetString(1);
                var matGroup = reader.GetString(2);
                var matCategory = reader.GetString(3);
                var qty = reader.GetInt32(4);
                var volume = reader.GetDouble(5);
                var consume = reader.GetInt32(6) == 1;
                productId = reader.GetInt64(7);

                materials.Add(new BlueprintMaterial(
                    matId, matName, matGroup, matCategory, qty, volume, consume));
            }
        }

        // Get products
        const string sqlProducts = @"
            SELECT p.productTypeID, t.typeName, p.quantity, p.probability
            FROM INDUSTRY_ACTIVITY_PRODUCTS p
            JOIN INVENTORY_TYPES t ON p.productTypeID = t.typeID
            WHERE p.blueprintTypeID = @blueprintId AND p.activityID = @activityId";

        var prodCmd = conn.CreateCommand();
        prodCmd.CommandText = sqlProducts;
        prodCmd.Parameters.AddWithValue("@blueprintId", blueprintId);
        prodCmd.Parameters.AddWithValue("@activityId", activityId);

        var products = new List<BlueprintProduct>();
        string productName = "Unknown";
        int productQuantity = 1;

        await using (var reader = await prodCmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var prodId = reader.GetInt64(0);
                var prodName = reader.GetString(1);
                var prodQty = reader.GetInt32(2);
                var probability = reader.GetDouble(3);

                products.Add(new BlueprintProduct(prodId, prodName, prodQty, probability));

                // Use first product as main product
                if (products.Count == 1)
                {
                    productId = prodId;
                    productName = prodName;
                    productQuantity = prodQty;
                }
            }
        }

        return new BlueprintActivity(
            activityId,
            activityName,
            productId,
            productName,
            productQuantity,
            materials,
            products);
    }

    private static string GetActivityName(int activityId)
    {
        return activityId switch
        {
            1 => "Manufacturing",
            3 => "Time Efficiency Research",
            4 => "Material Efficiency Research",
            5 => "Copying",
            8 => "Invention",
            11 => "Reactions",
            _ => $"Activity {activityId}"
        };
    }

    public async Task<RawMaterialsResponse?> GetRawMaterialsAsync(RawMaterialsRequest request, CancellationToken ct = default)
    {
        await using var conn = _connections.Create();
        await conn.OpenAsync(ct);

        // Get blueprint details
        var blueprintCmd = conn.CreateCommand();
        blueprintCmd.CommandText = @"
            SELECT BLUEPRINT_NAME 
            FROM ALL_BLUEPRINTS 
            WHERE BLUEPRINT_ID = @blueprintId";
        blueprintCmd.Parameters.AddWithValue("@blueprintId", request.BlueprintId);
        
        var blueprintName = await blueprintCmd.ExecuteScalarAsync(ct) as string;
        if (blueprintName is null) return null;

        // Get component materials (direct requirements)
        var componentMaterials = await GetComponentMaterialsAsync(conn, request.BlueprintId, request.MaterialEfficiency, request.Runs, ct);

        // Calculate raw materials by recursively breaking down components
        var rawMaterials = await CalculateRawMaterialsAsync(conn, request.BlueprintId, request.MaterialEfficiency, request.Runs, ct);

        return new RawMaterialsResponse(
            request.BlueprintId,
            blueprintName,
            componentMaterials,
            rawMaterials
        );
    }

    private async Task<List<MaterialBreakdown>> GetComponentMaterialsAsync(
        SqliteConnection conn, 
        long blueprintId, 
        int me, 
        int runs, 
        CancellationToken ct)
    {
        var materials = new List<MaterialBreakdown>();
        
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                m.MATERIAL_ID,
                m.MATERIAL,
                m.QUANTITY,
                CASE 
                    WHEN EXISTS (
                        SELECT 1 FROM ALL_BLUEPRINT_MATERIALS bm 
                        WHERE bm.PRODUCT_ID = m.MATERIAL_ID AND bm.ACTIVITY = 1
                    ) THEN 1 
                    ELSE 0 
                END as IS_MANUFACTURABLE,
                (
                    SELECT bp.BLUEPRINT_ID 
                    FROM ALL_BLUEPRINT_MATERIALS bm2
                    INNER JOIN ALL_BLUEPRINTS bp ON bp.BLUEPRINT_ID = bm2.BLUEPRINT_ID
                    WHERE bm2.PRODUCT_ID = m.MATERIAL_ID AND bm2.ACTIVITY = 1
                    LIMIT 1
                ) as COMPONENT_BLUEPRINT_ID
            FROM ALL_BLUEPRINT_MATERIALS m
            WHERE m.BLUEPRINT_ID = @blueprintId 
              AND m.ACTIVITY = 1
            ORDER BY m.MATERIAL";
        
        cmd.Parameters.AddWithValue("@blueprintId", blueprintId);
        
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var baseQuantity = reader.GetInt32(2);
            var adjustedQuantity = CalculateAdjustedQuantity(baseQuantity, me, runs);
            
            materials.Add(new MaterialBreakdown(
                reader.GetInt32(0), // MATERIAL_ID
                reader.GetString(1), // MATERIAL
                adjustedQuantity,
                reader.GetInt32(3) == 1, // IS_MANUFACTURABLE
                reader.IsDBNull(4) ? null : reader.GetInt32(4) // COMPONENT_BLUEPRINT_ID
            ));
        }
        
        return materials;
    }

    private async Task<List<MaterialBreakdown>> CalculateRawMaterialsAsync(
        SqliteConnection conn,
        long blueprintId,
        int me,
        int runs,
        CancellationToken ct)
    {
        var rawMaterialsDict = new Dictionary<int, (string Name, int Quantity, bool IsManufacturable)>();
        var visited = new HashSet<long>();
        
        await RecursivelyBreakdownMaterialsAsync(conn, blueprintId, me, runs, rawMaterialsDict, visited, ct);
        
        return rawMaterialsDict
            .Select(kvp => new MaterialBreakdown(
                kvp.Key,
                kvp.Value.Name,
                kvp.Value.Quantity,
                kvp.Value.IsManufacturable
            ))
            .OrderBy(m => m.TypeName)
            .ToList();
    }

    private async Task RecursivelyBreakdownMaterialsAsync(
        SqliteConnection conn,
        long blueprintId,
        int me,
        int runs,
        Dictionary<int, (string Name, int Quantity, bool IsManufacturable)> accumulator,
        HashSet<long> visited,
        CancellationToken ct)
    {
        // Prevent circular dependencies
        if (visited.Contains(blueprintId))
            return;
        
        visited.Add(blueprintId);

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                m.MATERIAL_ID,
                m.MATERIAL,
                m.QUANTITY,
                bp_component.BLUEPRINT_ID as COMPONENT_BLUEPRINT_ID
            FROM ALL_BLUEPRINT_MATERIALS m
            LEFT JOIN ALL_BLUEPRINT_MATERIALS bm_check ON bm_check.PRODUCT_ID = m.MATERIAL_ID AND bm_check.ACTIVITY = 1
            LEFT JOIN ALL_BLUEPRINTS bp_component ON bp_component.BLUEPRINT_ID = bm_check.BLUEPRINT_ID
            WHERE m.BLUEPRINT_ID = @blueprintId 
              AND m.ACTIVITY = 1
            GROUP BY m.MATERIAL_ID";
        
        cmd.Parameters.AddWithValue("@blueprintId", blueprintId);
        
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var materialId = reader.GetInt32(0);
            var materialName = reader.GetString(1);
            var baseQuantity = reader.GetInt32(2);
            var adjustedQuantity = CalculateAdjustedQuantity(baseQuantity, me, runs);
            var hasComponentBlueprint = !reader.IsDBNull(3);
            var componentBlueprintId = hasComponentBlueprint ? reader.GetInt64(3) : 0;

            if (hasComponentBlueprint && componentBlueprintId > 0)
            {
                // This material can be manufactured - recurse into its blueprint
                await RecursivelyBreakdownMaterialsAsync(
                    conn, 
                    componentBlueprintId, 
                    me, // Use same ME for components
                    adjustedQuantity, // Build this many units
                    accumulator, 
                    visited, 
                    ct);
            }
            else
            {
                // This is a raw material - add to accumulator
                if (accumulator.TryGetValue(materialId, out var existing))
                {
                    accumulator[materialId] = (existing.Name, existing.Quantity + adjustedQuantity, false);
                }
                else
                {
                    accumulator[materialId] = (materialName, adjustedQuantity, false);
                }
            }
        }
    }

    private static int CalculateAdjustedQuantity(int baseQuantity, int me, int runs)
    {
        // ME reduces material requirements by 1% per level
        var reduction = 1.0 - (me * 0.01);
        var perRun = (int)Math.Ceiling(baseQuantity * reduction);
        return perRun * runs;
    }
}
