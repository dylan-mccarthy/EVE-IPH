using server.Models;
using server.Services.Auth;

namespace server.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth");

        group.MapGet("/start", async (IAuthService auth, CancellationToken ct) =>
        {
            var result = await auth.StartAsync(ct);
            return Results.Ok(result);
        }).WithTags("Auth");

        group.MapPost("/exchange", async (AuthExchangeRequest request, IAuthService auth, CancellationToken ct) =>
        {
            try
            {
                var result = await auth.ExchangeAsync(request, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Invalid or expired state"))
            {
                return Results.BadRequest(new ApiError("invalid_state", "The authorization state is invalid or has expired. Please restart the login process."));
            }
            catch (HttpRequestException ex)
            {
                return Results.BadRequest(new ApiError("sso_error", $"EVE SSO error: {ex.Message}"));
            }
        }).WithTags("Auth");

        group.MapPost("/refresh/{characterId:long}", async (long characterId, ITokenRefreshService refreshService, CancellationToken ct) =>
        {
            var result = await refreshService.RefreshTokenIfNeededAsync(characterId, ct);
            
            if (!result.Success)
            {
                return Results.BadRequest(new ApiError("refresh_failed", result.Error ?? "Failed to refresh token"));
            }

            return Results.Ok(new
            {
                accessToken = result.AccessToken,
                expiresAt = result.ExpiresAt
            });
        }).WithTags("Auth");

        group.MapGet("/characters/{characterId:long}/token", async (long characterId, ITokenStore tokenStore, ITokenRefreshService refreshService, CancellationToken ct) =>
        {
            // Get token and refresh if needed
            var refreshResult = await refreshService.RefreshTokenIfNeededAsync(characterId, ct);
            
            if (!refreshResult.Success)
            {
                return Results.NotFound(new ApiError("token_not_found", "No valid token found for this character"));
            }

            return Results.Ok(new
            {
                characterId,
                accessToken = refreshResult.AccessToken,
                expiresAt = refreshResult.ExpiresAt
            });
        }).WithTags("Auth");

        return routes;
    }
}
