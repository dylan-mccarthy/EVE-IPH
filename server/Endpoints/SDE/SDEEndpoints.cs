using server.Services.SDE;

namespace server.Endpoints.SDE;

public static class SDEEndpoints
{
    public static IEndpointRouteBuilder MapSDEEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sde").WithTags("SDE");

        group.MapGet("/types/{typeId:long}", async (long typeId, ISDEService sdeService, CancellationToken ct) =>
        {
            var typeInfo = await sdeService.GetTypeInfoAsync(typeId, ct);
            return typeInfo is not null ? Results.Ok(typeInfo) : Results.NotFound();
        })
        .WithName("GetTypeInfo")
        .WithSummary("Get type information by type ID");

        group.MapPost("/types/batch", async (TypeIdBatchRequest request, ISDEService sdeService, CancellationToken ct) =>
        {
            var typeInfos = await sdeService.GetTypeInfoBatchAsync(request.TypeIds, ct);
            return Results.Ok(typeInfos);
        })
        .WithName("GetTypeInfoBatch")
        .WithSummary("Get multiple type information by type IDs");

        group.MapGet("/locations/{locationId:long}", async (long locationId, ISDEService sdeService, CancellationToken ct) =>
        {
            var locationName = await sdeService.GetLocationNameAsync(locationId, ct);
            return Results.Ok(new LocationNameResponse(locationName));
        })
        .WithName("GetLocationName")
        .WithSummary("Get location name by location ID");

        group.MapGet("/activities/{activityId:int}", async (int activityId, ISDEService sdeService, CancellationToken ct) =>
        {
            var activityName = await sdeService.GetActivityNameAsync(activityId, ct);
            return Results.Ok(new ActivityNameResponse(activityName));
        })
        .WithName("GetActivityName")
        .WithSummary("Get activity name by activity ID");

        return app;
    }
}

public sealed record TypeIdBatchRequest(List<long> TypeIds);
public sealed record LocationNameResponse(string Name);
public sealed record ActivityNameResponse(string Name);
