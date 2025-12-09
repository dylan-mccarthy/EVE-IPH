using Microsoft.AspNetCore.Mvc;
using server.Services.Assets;

namespace server.Endpoints.Assets;

public static class AssetsEndpoints
{
    public static void MapAssetsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/characters/{characterId:long}/assets")
            .WithTags("Assets");

        group.MapGet("", GetAssets)
            .WithName("GetAssets")
            .WithOpenApi();
    }

    private static async Task<IResult> GetAssets(
        [FromRoute] long characterId,
        [FromServices] IAssetsService assetsService,
        CancellationToken ct)
    {
        try
        {
            var assets = await assetsService.GetAssetsAsync(characterId, ct);
            return Results.Ok(assets);
        }
        catch (InvalidOperationException)
        {
            return Results.Unauthorized();
        }
        catch (HttpRequestException ex)
        {
            return Results.Problem(ex.Message, statusCode: 502);
        }
    }
}
