using Microsoft.AspNetCore.Http.HttpResults;
using server.Infrastructure;
using server.Models;

namespace server.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/health", Health).WithName("Health");
        routes.MapGet("/version", Version).WithName("Version");
        return routes;
    }

    private static Ok<HealthResponse> Health(AppInfo info)
    {
        var response = new HealthResponse("OK", DateTimeOffset.UtcNow, info.Environment);
        return TypedResults.Ok(response);
    }

    private static Ok<VersionResponse> Version(AppInfo info)
    {
        var response = new VersionResponse(info.Name, info.Version, info.Environment, info.StartedUtc);
        return TypedResults.Ok(response);
    }
}
