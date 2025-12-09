using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using EveIph.Server.Models;
using server.Infrastructure;

namespace EveIph.Server.Services.Market;

public class MarketPriceService : IMarketPriceService
{
    private readonly ISqliteConnectionFactory _dbFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MarketPriceService> _logger;
    private readonly SemaphoreSlim _rateLimiter = new(10, 10); // Max 10 concurrent ESI requests
    private const string ESI_BASE_URL = "https://esi.evetech.net/latest";
    private const int CACHE_DURATION_SECONDS = 3600; // 1 hour

    public MarketPriceService(
        ISqliteConnectionFactory dbFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<MarketPriceService> logger)
    {
        _dbFactory = dbFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        
        // Ensure market_prices table exists
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var db = _dbFactory.Create();
        db.Open();
        
        var cmd = db.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS MARKET_PRICES (
                TYPE_ID INTEGER PRIMARY KEY,
                REGION_ID INTEGER,
                BUY_PRICE REAL,
                SELL_PRICE REAL,
                VOLUME INTEGER,
                LAST_UPDATED TEXT,
                EXPIRES_AT TEXT
            )";
        cmd.ExecuteNonQuery();
        
        cmd.CommandText = @"
            CREATE INDEX IF NOT EXISTS idx_market_prices_expires 
            ON MARKET_PRICES(EXPIRES_AT)";
        cmd.ExecuteNonQuery();
    }

    public async Task<MarketPrice?> GetPriceAsync(int typeId, int regionId = 10000002)
    {
        var prices = await GetPricesAsync(new[] { typeId }, regionId);
        return prices.TryGetValue(typeId, out var price) ? price : null;
    }

    public async Task<Dictionary<int, MarketPrice>> GetPricesAsync(IEnumerable<int> typeIds, int regionId = 10000002)
    {
        var typeIdList = typeIds.ToList();
        var result = new Dictionary<int, MarketPrice>();
        var toFetch = new List<int>();

        // Check cache first
        await using (var db = _dbFactory.Create())
        {
            await db.OpenAsync();
            var now = DateTime.UtcNow;
            
            foreach (var typeId in typeIdList)
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = @"
                    SELECT TYPE_ID, REGION_ID, BUY_PRICE, SELL_PRICE, VOLUME, LAST_UPDATED, EXPIRES_AT
                    FROM MARKET_PRICES
                    WHERE TYPE_ID = @TypeId AND REGION_ID = @RegionId";
                cmd.Parameters.AddWithValue("@TypeId", typeId);
                cmd.Parameters.AddWithValue("@RegionId", regionId);
                
                await using var reader = await cmd.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var expiresAt = DateTime.Parse(reader.GetString(6));
                    
                    if (expiresAt > now)
                    {
                        // Cache is still valid
                        result[typeId] = new MarketPrice(
                            reader.GetInt32(0),  // TYPE_ID
                            reader.GetInt32(1),  // REGION_ID
                            reader.GetDecimal(2), // BUY_PRICE
                            reader.GetDecimal(3), // SELL_PRICE
                            reader.GetInt64(4),   // VOLUME
                            DateTime.Parse(reader.GetString(5)), // LAST_UPDATED
                            expiresAt
                        );
                    }
                    else
                    {
                        // Cache expired, need to fetch
                        toFetch.Add(typeId);
                    }
                }
                else
                {
                    // Not in cache at all
                    toFetch.Add(typeId);
                }
            }
        }

        // Fetch missing/expired prices from ESI
        if (toFetch.Any())
        {
            var (fetched, _) = await RefreshPricesAsync(toFetch, regionId);
            
            // Get newly cached prices
            await using var db = _dbFactory.Create();
            await db.OpenAsync();
            
            foreach (var typeId in toFetch)
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = @"
                    SELECT TYPE_ID, REGION_ID, BUY_PRICE, SELL_PRICE, VOLUME, LAST_UPDATED, EXPIRES_AT
                    FROM MARKET_PRICES
                    WHERE TYPE_ID = @TypeId AND REGION_ID = @RegionId";
                cmd.Parameters.AddWithValue("@TypeId", typeId);
                cmd.Parameters.AddWithValue("@RegionId", regionId);
                
                await using var reader = await cmd.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    result[typeId] = new MarketPrice(
                        reader.GetInt32(0),
                        reader.GetInt32(1),
                        reader.GetDecimal(2),
                        reader.GetDecimal(3),
                        reader.GetInt64(4),
                        DateTime.Parse(reader.GetString(5)),
                        DateTime.Parse(reader.GetString(6))
                    );
                }
            }
        }

        return result;
    }

    public async Task<(int updated, int failed)> RefreshPricesAsync(IEnumerable<int> typeIds, int regionId = 10000002)
    {
        var typeIdList = typeIds.ToList();
        var updated = 0;
        var failed = 0;

        try
        {
            // Fetch all orders for the region in one request
            _logger.LogInformation("Fetching market orders for region {RegionId} to update {Count} types", regionId, typeIdList.Count);
            
            var allOrders = await FetchAllOrdersForRegion(regionId);
            
            if (allOrders == null || allOrders.Count == 0)
            {
                _logger.LogWarning("No market orders found for region {RegionId}", regionId);
                return (0, typeIdList.Count);
            }

            // Group orders by type ID
            var ordersByType = allOrders
                .Where(o => typeIdList.Contains(o.TypeId))
                .GroupBy(o => o.TypeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Process each type
            foreach (var typeId in typeIdList)
            {
                try
                {
                    if (ordersByType.TryGetValue(typeId, out var orders) && orders.Any())
                    {
                        var buyOrders = orders.Where(o => o.IsBuyOrder).ToList();
                        var sellOrders = orders.Where(o => !o.IsBuyOrder).ToList();

                        var buyPrice = buyOrders.Any() ? buyOrders.Max(o => o.Price) : 0m;
                        var sellPrice = sellOrders.Any() ? sellOrders.Min(o => o.Price) : 0m;
                        var volume = orders.Sum(o => o.VolumeRemain);

                        var now = DateTime.UtcNow;
                        var price = new MarketPrice(
                            typeId,
                            regionId,
                            buyPrice,
                            sellPrice,
                            volume,
                            now,
                            now.AddSeconds(CACHE_DURATION_SECONDS)
                        );

                        await SavePriceToCache(price);
                        updated++;
                    }
                    else
                    {
                        _logger.LogDebug("No market orders found for type {TypeId}", typeId);
                        failed++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process price for type {TypeId}", typeId);
                    failed++;
                }
            }

            _logger.LogInformation("Price update complete: {Updated} updated, {Failed} failed", updated, failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch market orders for region {RegionId}", regionId);
            return (0, typeIdList.Count);
        }

        return (updated, failed);
    }

    private async Task<List<EsiMarketOrder>?> FetchAllOrdersForRegion(int regionId)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "EVE-IPH-Modernization/1.0");
        
        var allOrders = new List<EsiMarketOrder>();
        var page = 1;
        var maxPages = 100; // Safety limit

        while (page <= maxPages)
        {
            var url = $"{ESI_BASE_URL}/markets/{regionId}/orders/?order_type=all&page={page}";
            
            try
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                // Check if there are more pages
                var totalPagesHeader = response.Headers.GetValues("X-Pages").FirstOrDefault();
                if (totalPagesHeader != null && int.TryParse(totalPagesHeader, out var totalPages))
                {
                    maxPages = Math.Min(totalPages, 100); // Cap at 100 pages for safety
                }
                
                var json = await response.Content.ReadAsStringAsync();
                var orders = JsonSerializer.Deserialize<List<EsiMarketOrder>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (orders != null && orders.Any())
                {
                    allOrders.AddRange(orders);
                    _logger.LogDebug("Fetched page {Page} with {Count} orders", page, orders.Count);
                }
                else
                {
                    break; // No more data
                }

                page++;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "ESI request failed for region {RegionId} page {Page}", regionId, page);
                break;
            }
        }

        _logger.LogInformation("Fetched {TotalOrders} total orders from {Pages} pages for region {RegionId}", 
            allOrders.Count, page - 1, regionId);
        
        return allOrders.Any() ? allOrders : null;
    }

    private async Task SavePriceToCache(MarketPrice price)
    {
        await using var db = _dbFactory.Create();
        await db.OpenAsync();
        
        var cmd = db.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO MARKET_PRICES 
            (TYPE_ID, REGION_ID, BUY_PRICE, SELL_PRICE, VOLUME, LAST_UPDATED, EXPIRES_AT)
            VALUES 
            (@TypeId, @RegionId, @BuyPrice, @SellPrice, @Volume, @LastUpdated, @ExpiresAt)";
        
        cmd.Parameters.AddWithValue("@TypeId", price.TypeId);
        cmd.Parameters.AddWithValue("@RegionId", price.RegionId);
        cmd.Parameters.AddWithValue("@BuyPrice", price.BuyPrice);
        cmd.Parameters.AddWithValue("@SellPrice", price.SellPrice);
        cmd.Parameters.AddWithValue("@Volume", price.Volume);
        cmd.Parameters.AddWithValue("@LastUpdated", price.LastUpdated.ToString("O"));
        cmd.Parameters.AddWithValue("@ExpiresAt", price.ExpiresAt.ToString("O"));
        
        await cmd.ExecuteNonQueryAsync();
    }

    // ESI response models
    private record EsiMarketOrder(
        [property: JsonPropertyName("order_id")] long OrderId,
        [property: JsonPropertyName("type_id")] int TypeId,
        [property: JsonPropertyName("location_id")] long LocationId,
        [property: JsonPropertyName("volume_total")] long VolumeTotal,
        [property: JsonPropertyName("volume_remain")] long VolumeRemain,
        [property: JsonPropertyName("min_volume")] long MinVolume,
        [property: JsonPropertyName("price")] decimal Price,
        [property: JsonPropertyName("is_buy_order")] bool IsBuyOrder,
        [property: JsonPropertyName("duration")] int Duration,
        [property: JsonPropertyName("issued")] DateTime Issued,
        [property: JsonPropertyName("range")] string Range
    );
}
