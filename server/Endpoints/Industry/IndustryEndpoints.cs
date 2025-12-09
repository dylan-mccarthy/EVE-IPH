using Microsoft.AspNetCore.Mvc;
using server.Services.Industry;

namespace server.Endpoints.Industry;

public static class IndustryEndpoints
{
    public static void MapIndustryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/characters/{characterId:long}/industry")
            .WithTags("Industry");

        group.MapGet("/jobs", GetIndustryJobs)
            .WithName("GetIndustryJobs")
            .WithOpenApi();
    }

    private static async Task<IResult> GetIndustryJobs(
        [FromRoute] long characterId,
        [FromQuery] bool includeCompleted,
        [FromServices] IIndustryService industryService,
        CancellationToken ct)
    {
        try
        {
            var jobs = await industryService.GetIndustryJobsAsync(characterId, includeCompleted, ct);
            return Results.Ok(jobs);
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
