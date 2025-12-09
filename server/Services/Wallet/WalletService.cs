using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using server.Services.Auth;

namespace server.Services.Wallet;

public sealed class WalletService : IWalletService
{
    private readonly HttpClient _client;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<WalletService> _logger;

    public WalletService(HttpClient client, ITokenStore tokenStore, ILogger<WalletService> logger)
    {
        _client = client;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    public async Task<decimal> GetWalletBalanceAsync(long characterId, CancellationToken ct = default)
    {
        var token = await _tokenStore.GetTokenAsync(characterId, ct);
        if (token == null)
        {
            _logger.LogWarning("No token found for character {CharacterId}", characterId);
            throw new InvalidOperationException($"No authentication token found for character {characterId}");
        }

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        var response = await _client.GetAsync($"characters/{characterId}/wallet/?datasource=tranquility", ct);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch wallet balance for character {CharacterId}: {StatusCode}", 
                characterId, response.StatusCode);
            throw new HttpRequestException($"ESI request failed with status {response.StatusCode}");
        }

        var balance = await response.Content.ReadFromJsonAsync<decimal>(ct);
        return balance;
    }

    public async Task<List<WalletTransaction>> GetWalletTransactionsAsync(long characterId, CancellationToken ct = default)
    {
        var token = await _tokenStore.GetTokenAsync(characterId, ct);
        if (token == null)
        {
            _logger.LogWarning("No token found for character {CharacterId}", characterId);
            throw new InvalidOperationException($"No authentication token found for character {characterId}");
        }

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        var response = await _client.GetAsync($"characters/{characterId}/wallet/transactions/?datasource=tranquility", ct);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch wallet transactions for character {CharacterId}: {StatusCode}", 
                characterId, response.StatusCode);
            return new List<WalletTransaction>();
        }

        var esiData = await response.Content.ReadFromJsonAsync<List<EsiWalletTransaction>>(ct);
        return esiData?.Select(t => new WalletTransaction(
            t.TransactionId,
            t.Date,
            t.TypeId,
            t.LocationId,
            t.Quantity,
            t.UnitPrice,
            t.ClientId,
            t.IsBuy,
            t.IsPersonal,
            t.JournalRefId
        )).ToList() ?? new List<WalletTransaction>();
    }

    public async Task<List<WalletJournalEntry>> GetWalletJournalAsync(long characterId, CancellationToken ct = default)
    {
        var token = await _tokenStore.GetTokenAsync(characterId, ct);
        if (token == null)
        {
            _logger.LogWarning("No token found for character {CharacterId}", characterId);
            throw new InvalidOperationException($"No authentication token found for character {characterId}");
        }

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        var response = await _client.GetAsync($"characters/{characterId}/wallet/journal/?datasource=tranquility", ct);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch wallet journal for character {CharacterId}: {StatusCode}", 
                characterId, response.StatusCode);
            return new List<WalletJournalEntry>();
        }

        var esiData = await response.Content.ReadFromJsonAsync<List<EsiWalletJournalEntry>>(ct);
        return esiData?.Select(j => new WalletJournalEntry(
            j.Id,
            j.Date,
            j.RefType,
            j.FirstPartyId,
            j.SecondPartyId,
            j.Amount,
            j.Balance,
            j.Description ?? string.Empty,
            j.Reason ?? string.Empty,
            j.TaxReceiverId,
            j.Tax
        )).ToList() ?? new List<WalletJournalEntry>();
    }

    // ESI response models
    private sealed record EsiWalletTransaction(
        [property: JsonPropertyName("transaction_id")] long TransactionId,
        [property: JsonPropertyName("date")] DateTimeOffset Date,
        [property: JsonPropertyName("type_id")] long TypeId,
        [property: JsonPropertyName("location_id")] long LocationId,
        [property: JsonPropertyName("quantity")] int Quantity,
        [property: JsonPropertyName("unit_price")] decimal UnitPrice,
        [property: JsonPropertyName("client_id")] long ClientId,
        [property: JsonPropertyName("is_buy")] bool IsBuy,
        [property: JsonPropertyName("is_personal")] bool IsPersonal,
        [property: JsonPropertyName("journal_ref_id")] long JournalRefId
    );

    private sealed record EsiWalletJournalEntry(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("date")] DateTimeOffset Date,
        [property: JsonPropertyName("ref_type")] string RefType,
        [property: JsonPropertyName("first_party_id")] long? FirstPartyId,
        [property: JsonPropertyName("second_party_id")] long? SecondPartyId,
        [property: JsonPropertyName("amount")] decimal? Amount,
        [property: JsonPropertyName("balance")] decimal? Balance,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("reason")] string? Reason,
        [property: JsonPropertyName("tax_receiver_id")] long? TaxReceiverId,
        [property: JsonPropertyName("tax")] decimal? Tax
    );
}
