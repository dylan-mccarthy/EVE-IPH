using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using server.Models;
using server.Services.Market;
using EveIph.Server.Models;
using EveIph.Server.Services.Market;
using server.Infrastructure;

namespace server.Endpoints.Market;

public static class MarketEndpoints
{
    public static IEndpointRouteBuilder MapMarketEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/market").WithTags("Market");

        group.MapGet("/prices", GetPrices)
            .WithName("GetPrices")
            .WithSummary("Fetch market prices for requested items.")
            .Produces<MarketPricesResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        // New: Cached market prices from ESI
        group.MapGet("/prices/cached", GetCachedPrices)
            .WithName("GetCachedMarketPrices")
            .WithSummary("Get cached market prices for multiple type IDs")
            .WithOpenApi();

        group.MapGet("/prices/cached/{typeId}", GetCachedPrice)
            .WithName("GetCachedMarketPrice")
            .WithSummary("Get cached market price for a single type ID")
            .WithOpenApi();

        group.MapPost("/prices/refresh", RefreshPrices)
            .WithName("RefreshMarketPrices")
            .WithSummary("Force refresh market prices from ESI")
            .WithOpenApi();

        // Item groups endpoints
        group.MapGet("/groups", GetItemGroups)
            .WithName("GetItemGroups")
            .WithSummary("Get all tradeable item groups")
            .WithOpenApi();

        group.MapPost("/groups/items", GetItemsByGroups)
            .WithName("GetItemsByGroups")
            .WithSummary("Get all type IDs for selected item groups")
            .WithOpenApi();

        // Character market orders endpoint
        var charGroup = routes.MapGroup("/api/characters/{characterId:long}/orders").WithTags("Market");
        
        charGroup.MapGet("", GetCharacterOrders)
            .WithName("GetCharacterOrders")
            .WithSummary("Fetch market orders for a character.")
            .WithOpenApi();

        return routes;
    }

    private static async Task<Ok<MarketPricesResponse>> GetPrices(
        [FromServices] IMarketService service,
        [FromQuery(Name = "typeIds")] long[] typeIds,
        [FromQuery] string region = "The Forge",
        [FromQuery(Name = "system")] string systemName = "Jita")
    {
        var request = new MarketPricesRequest(typeIds, region, systemName);
        var result = await service.GetPricesAsync(request);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetCharacterOrders(
        [FromRoute] long characterId,
        [FromServices] IMarketOrdersService service,
        CancellationToken ct)
    {
        try
        {
            var orders = await service.GetMarketOrdersAsync(characterId, ct);
            return Results.Ok(orders);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    // New cached price endpoints
    private static async Task<IResult> GetCachedPrices(
        [FromQuery] string typeIds,
        [FromQuery] int regionId,
        [FromServices] IMarketPriceService priceService)
    {
        if (string.IsNullOrWhiteSpace(typeIds))
        {
            return Results.BadRequest("typeIds parameter is required");
        }

        // Parse comma-separated type IDs
        var ids = typeIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => int.TryParse(id.Trim(), out var parsed) ? parsed : -1)
            .Where(id => id > 0)
            .ToList();

        if (ids.Count == 0)
        {
            return Results.BadRequest("No valid type IDs provided");
        }

        var prices = await priceService.GetPricesAsync(ids, regionId);
        
        return Results.Ok(new MarketPriceResponse(prices));
    }

    private static async Task<IResult> GetCachedPrice(
        int typeId,
        [FromQuery] int regionId,
        [FromServices] IMarketPriceService priceService)
    {
        if (typeId <= 0)
        {
            return Results.BadRequest("Invalid type ID");
        }

        var price = await priceService.GetPriceAsync(typeId, regionId);
        
        if (price == null)
        {
            return Results.NotFound($"No market data found for type {typeId} in region {regionId}");
        }

        return Results.Ok(price);
    }

    private static async Task<IResult> RefreshPrices(
        MarketPriceRequest request,
        [FromServices] IMarketPriceService priceService)
    {
        if (request.TypeIds == null || request.TypeIds.Length == 0)
        {
            return Results.BadRequest("TypeIds array is required");
        }

        var (updated, failed) = await priceService.RefreshPricesAsync(request.TypeIds, request.RegionId);
        
        return Results.Ok(new RefreshPricesResponse(updated, failed));
    }

    private static async Task<IResult> GetItemGroups(
        [FromServices] ISqliteConnectionFactory dbFactory)
    {
        await using var conn = dbFactory.Create();
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                g.groupID,
                g.groupName,
                g.categoryID,
                COUNT(t.typeID) as itemCount
            FROM INVENTORY_GROUPS g
            JOIN INVENTORY_TYPES t ON g.groupID = t.groupID
            WHERE t.marketGroupID IS NOT NULL
            GROUP BY g.groupID, g.groupName, g.categoryID
            ORDER BY g.groupName";

        var groups = new List<ItemGroup>();
        await using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            groups.Add(new ItemGroup(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetInt32(3)
            ));
        }

        return Results.Ok(new ItemGroupsResponse(groups));
    }

    private static async Task<IResult> GetItemsByGroups(
        ItemsByGroupRequest request,
        [FromServices] ISqliteConnectionFactory dbFactory)
    {
        if (request.GroupIds == null || request.GroupIds.Length == 0)
        {
            return Results.BadRequest("GroupIds array is required");
        }

        await using var conn = dbFactory.Create();
        await conn.OpenAsync();

        var placeholders = string.Join(",", request.GroupIds.Select((_, i) => $"@groupId{i}"));
        var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            SELECT DISTINCT t.typeID
            FROM INVENTORY_TYPES t
            WHERE t.groupID IN ({placeholders})
              AND t.marketGroupID IS NOT NULL
            ORDER BY t.typeID";

        for (int i = 0; i < request.GroupIds.Length; i++)
        {
            cmd.Parameters.AddWithValue($"@groupId{i}", request.GroupIds[i]);
        }

        var typeIds = new List<int>();
        await using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            typeIds.Add(reader.GetInt32(0));
        }

        return Results.Ok(new ItemsByGroupResponse(typeIds.Count, typeIds));
    }
}
