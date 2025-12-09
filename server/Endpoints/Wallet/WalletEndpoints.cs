using Microsoft.AspNetCore.Mvc;
using server.Services.Wallet;

namespace server.Endpoints.Wallet;

public static class WalletEndpoints
{
    public static void MapWalletEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/characters/{characterId:long}/wallet")
            .WithTags("Wallet");

        group.MapGet("/balance", GetWalletBalance)
            .WithName("GetWalletBalance")
            .WithOpenApi();

        group.MapGet("/transactions", GetWalletTransactions)
            .WithName("GetWalletTransactions")
            .WithOpenApi();

        group.MapGet("/journal", GetWalletJournal)
            .WithName("GetWalletJournal")
            .WithOpenApi();
    }

    private static async Task<IResult> GetWalletBalance(
        [FromRoute] long characterId,
        [FromServices] IWalletService walletService,
        CancellationToken ct)
    {
        try
        {
            var balance = await walletService.GetWalletBalanceAsync(characterId, ct);
            return Results.Ok(new { balance });
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

    private static async Task<IResult> GetWalletTransactions(
        [FromRoute] long characterId,
        [FromServices] IWalletService walletService,
        CancellationToken ct)
    {
        try
        {
            var transactions = await walletService.GetWalletTransactionsAsync(characterId, ct);
            return Results.Ok(transactions);
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

    private static async Task<IResult> GetWalletJournal(
        [FromRoute] long characterId,
        [FromServices] IWalletService walletService,
        CancellationToken ct)
    {
        try
        {
            var journal = await walletService.GetWalletJournalAsync(characterId, ct);
            return Results.Ok(journal);
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
