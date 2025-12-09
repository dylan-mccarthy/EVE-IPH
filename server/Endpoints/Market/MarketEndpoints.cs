using Microsoft.AspNetCore.Http.HttpResults;
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

    private static async Task<Ok<MarketPricesResponse>> GetPrices(IMarketService service, [AsParameters] MarketPricesRequest request)
    {
        var result = await service.GetPricesAsync(request);
        return TypedResults.Ok(result);
    }
}
