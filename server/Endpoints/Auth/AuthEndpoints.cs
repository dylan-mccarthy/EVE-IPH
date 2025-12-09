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

        return routes;
    }
}
