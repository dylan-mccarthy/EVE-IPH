using server.Models;
using server.Services.Characters;

namespace server.Endpoints.Characters;

public static class CharacterEndpoints
{
    public static IEndpointRouteBuilder MapCharacterEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/characters");

        group.MapPost("/{characterId:long}/sync", async (long characterId, CharacterSyncRequest request, ICharacterService characters, CancellationToken ct) =>
        {
            var profile = await characters.GetProfileAsync(characterId, request.AccessToken, ct);
            return Results.Ok(profile);
        }).WithTags("Characters");

        group.MapGet("/{characterId:long}/skills", async (long characterId, string accessToken, ICharacterService characters, CancellationToken ct) =>
        {
            var skills = await characters.GetSkillsAsync(characterId, accessToken, ct);
            return Results.Ok(skills);
        }).WithTags("Characters");

        return routes;
    }
}

