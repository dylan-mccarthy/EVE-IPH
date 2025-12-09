using Microsoft.AspNetCore.Http.HttpResults;
using server.Models;
using server.Services.Manufacturing;

namespace server.Endpoints.Manufacturing;

public static class ManufacturingEndpoints
{
    public static IEndpointRouteBuilder MapManufacturingEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/manufacturing").WithTags("Manufacturing");

        group.MapPost("/calculate", Calculate)
            .WithName("CalculateManufacturing")
            .WithSummary("Run manufacturing calculations for a blueprint and settings.")
            .Produces<ManufacturingResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        return routes;
    }

    private static async Task<Ok<ManufacturingResponse>> Calculate(IManufacturingService service, ManufacturingRequest request)
    {
        var result = await service.CalculateAsync(request);
        return TypedResults.Ok(result);
    }
}
