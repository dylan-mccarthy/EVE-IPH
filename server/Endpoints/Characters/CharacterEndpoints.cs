using server.Models;
using server.Services.Auth;
using server.Services.Characters;

namespace server.Endpoints.Characters;

public static class CharacterEndpoints
{
    public static IEndpointRouteBuilder MapCharacterEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/characters");

        group.MapGet("/", async (ICharacterService characters, CancellationToken ct) =>
        {
            var result = await characters.GetCharactersAsync(ct);
            return Results.Ok(result);
        }).WithTags("Characters");

        group.MapGet("/{characterId:long}", async (long characterId, ICharacterService characters, CancellationToken ct) =>
        {
            try
            {
                var details = await characters.GetCharacterDetailsAsync(characterId, ct);
                return Results.Ok(details);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new ApiError("character_not_found", ex.Message));
            }
        }).WithTags("Characters");

        group.MapPost("/{characterId:long}/sync", async (long characterId, CharacterSyncRequest request, ICharacterService characters, CancellationToken ct) =>
        {
            var profile = await characters.GetProfileAsync(characterId, request.AccessToken, ct);
            return Results.Ok(profile);
        }).WithTags("Characters");

        group.MapGet("/{characterId:long}/skills", async (long characterId, ITokenRefreshService refreshService, ICharacterService characters, CancellationToken ct) =>
        {
            // Get and refresh token if needed
            var tokenResult = await refreshService.RefreshTokenIfNeededAsync(characterId, ct);
            
            if (!tokenResult.Success || tokenResult.AccessToken is null)
            {
                return Results.Unauthorized();
            }

            var skills = await characters.GetSkillsAsync(characterId, tokenResult.AccessToken, ct);
            return Results.Ok(skills);
        }).WithTags("Characters");

        return routes;
    }
}

