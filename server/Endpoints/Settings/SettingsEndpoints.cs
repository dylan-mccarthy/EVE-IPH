using Microsoft.AspNetCore.Http.HttpResults;
using server.Models;
using server.Services.Settings;

namespace server.Endpoints.Settings;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/settings").WithTags("Settings");

        group.MapGet("", Get)
            .WithName("GetSettings")
            .WithSummary("Get user/application settings.")
            .Produces<SettingsResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        group.MapPut("", Save)
            .WithName("SaveSettings")
            .WithSummary("Save user/application settings.")
            .Produces<SettingsResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        return routes;
    }

    private static async Task<Ok<SettingsResponse>> Get(ISettingsService service)
    {
        var result = await service.GetAsync();
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<SettingsResponse>> Save(ISettingsService service, SettingsRequest request)
    {
        var result = await service.SaveAsync(request);
        return TypedResults.Ok(result);
    }
}
