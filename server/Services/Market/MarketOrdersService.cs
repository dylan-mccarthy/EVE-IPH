using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using server.Infrastructure;
using server.Services.Auth;

namespace server.Services.Market;

public sealed class MarketOrdersService : IMarketOrdersService
{
    private readonly HttpClient _http;
    private readonly EsiOptions _options;
    private readonly ITokenRefreshService _tokenRefresh;
    private readonly ILogger<MarketOrdersService> _logger;

    public MarketOrdersService(
        HttpClient http,
        IOptions<EsiOptions> options,
        ITokenRefreshService tokenRefresh,
        ILogger<MarketOrdersService> logger)
    {
        _http = http;
        _options = options.Value;
        _tokenRefresh = tokenRefresh;
        _logger = logger;
        _http.BaseAddress ??= new Uri(_options.BaseUrl);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<List<MarketOrder>> GetMarketOrdersAsync(long characterId, CancellationToken ct = default)
    {
        var tokenResult = await _tokenRefresh.RefreshTokenIfNeededAsync(characterId, ct);
        if (!tokenResult.Success || tokenResult.AccessToken is null)
        {
            _logger.LogWarning("Token refresh failed for character {CharacterId}", characterId);
            return new List<MarketOrder>();
        }

        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"characters/{characterId}/orders/?datasource=tranquility");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

            using var response = await _http.SendAsync(request, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Failed to fetch market orders for character {CharacterId}: {StatusCode} - {Error}",
                    characterId, response.StatusCode, errorContent);
                return new List<MarketOrder>();
            }

            var esiOrders = await response.Content.ReadFromJsonAsync<List<EsiMarketOrder>>(cancellationToken: ct);
            if (esiOrders is null)
            {
                return new List<MarketOrder>();
            }

            return esiOrders.Select(o => new MarketOrder(
                o.order_id,
                o.type_id,
                o.location_id,
                o.region_id,
                o.volume_total.ToString(),
                o.volume_remain.ToString(),
                o.min_volume.ToString(),
                o.price,
                o.is_buy_order,
                o.duration.ToString(),
                o.issued,
                o.range
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching market orders for character {CharacterId}", characterId);
            return new List<MarketOrder>();
        }
    }

    private sealed record EsiMarketOrder(
        long order_id,
        long type_id,
        long location_id,
        long region_id,
        int volume_total,
        int volume_remain,
        int min_volume,
        decimal price,
        bool is_buy_order,
        int duration,
        DateTimeOffset issued,
        string range
    );
}
