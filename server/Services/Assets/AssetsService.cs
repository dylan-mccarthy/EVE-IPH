using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using server.Services.Auth;

namespace server.Services.Assets;

public sealed class AssetsService : IAssetsService
{
    private readonly HttpClient _client;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<AssetsService> _logger;

    public AssetsService(HttpClient client, ITokenStore tokenStore, ILogger<AssetsService> logger)
    {
        _client = client;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    public async Task<List<Asset>> GetAssetsAsync(long characterId, CancellationToken ct = default)
    {
        var token = await _tokenStore.GetTokenAsync(characterId, ct);
        if (token == null)
        {
            _logger.LogWarning("No token found for character {CharacterId}", characterId);
            throw new InvalidOperationException($"No authentication token found for character {characterId}");
        }

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        var allAssets = new List<Asset>();
        var page = 1;
        
        // ESI returns assets in pages
        while (true)
        {
            var response = await _client.GetAsync(
                $"characters/{characterId}/assets/?datasource=tranquility&page={page}", ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch assets for character {CharacterId}: {StatusCode}", 
                    characterId, response.StatusCode);
                break;
            }

            var pageAssets = await response.Content.ReadFromJsonAsync<List<EsiAsset>>(ct);
            if (pageAssets == null || pageAssets.Count == 0)
            {
                break;
            }

            allAssets.AddRange(pageAssets.Select(a => new Asset(
                a.ItemId,
                a.LocationId,
                a.LocationFlag,
                a.LocationType,
                a.TypeId,
                a.Quantity,
                a.IsSingleton,
                a.IsBlueprintCopy
            )));

            // Check if there are more pages
            if (!response.Headers.Contains("X-Pages") || 
                !int.TryParse(response.Headers.GetValues("X-Pages").FirstOrDefault(), out var totalPages) ||
                page >= totalPages)
            {
                break;
            }

            page++;
        }

        return allAssets;
    }

    private sealed record EsiAsset(
        [property: JsonPropertyName("item_id")] long ItemId,
        [property: JsonPropertyName("location_id")] long LocationId,
        [property: JsonPropertyName("location_flag")] string LocationFlag,
        [property: JsonPropertyName("location_type")] string LocationType,
        [property: JsonPropertyName("type_id")] int TypeId,
        [property: JsonPropertyName("quantity")] int Quantity,
        [property: JsonPropertyName("is_singleton")] bool IsSingleton,
        [property: JsonPropertyName("is_blueprint_copy")] bool? IsBlueprintCopy
    );
}
