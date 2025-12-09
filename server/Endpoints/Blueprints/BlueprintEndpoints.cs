using Microsoft.AspNetCore.Http.HttpResults;
using server.Models;
using server.Services.Blueprints;

namespace server.Endpoints.Blueprints;

public static class BlueprintEndpoints
{
    public static IEndpointRouteBuilder MapBlueprintEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/blueprints").WithTags("Blueprints");

        group.MapGet("/search", Search)
            .WithName("SearchBlueprints")
            .WithSummary("Search blueprints by name, group, or category.")
            .Produces<BlueprintSearchResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        group.MapGet("/{blueprintId:long}", GetDetails)
            .WithName("GetBlueprintDetails")
            .WithSummary("Get detailed blueprint information including materials and products.")
            .Produces<BlueprintDetails>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        group.MapPost("/{blueprintId:long}/raw-materials", GetRawMaterials)
            .WithName("GetRawMaterials")
            .WithSummary("Calculate raw materials breakdown by recursively expanding components.")
            .Produces<RawMaterialsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        return routes;
    }

    private static async Task<Ok<BlueprintSearchResponse>> Search(IBlueprintService service, [AsParameters] BlueprintSearchRequest request)
    {
        var result = await service.SearchAsync(request);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<BlueprintDetails>, NotFound>> GetDetails(
        long blueprintId,
        IBlueprintService service,
        CancellationToken ct)
    {
        var result = await service.GetDetailsAsync(blueprintId, ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<RawMaterialsResponse>, NotFound>> GetRawMaterials(
        long blueprintId,
        RawMaterialsRequest request,
        IBlueprintService service,
        CancellationToken ct)
    {
        // Override blueprint ID from route
        var adjustedRequest = request with { BlueprintId = blueprintId };
        var result = await service.GetRawMaterialsAsync(adjustedRequest, ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
