using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using server.Models;
using server.Services.Market;

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
}
