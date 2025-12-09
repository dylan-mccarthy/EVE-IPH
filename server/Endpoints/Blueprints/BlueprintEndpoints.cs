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

        return routes;
    }

    private static async Task<Ok<BlueprintSearchResponse>> Search(IBlueprintService service, [AsParameters] BlueprintSearchRequest request)
    {
        var result = await service.SearchAsync(request);
        return TypedResults.Ok(result);
    }
}
