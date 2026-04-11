using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Infrastructure.ESI.Market;

public sealed class TranquilityMarketPriceSource(HttpClient httpClient) : IMarketPriceSource
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public MarketPriceSourceKind SourceKind => MarketPriceSourceKind.Tranquility;

    public async Task<Result<IReadOnlyDictionary<TypeId, MarketPrice>>> GetPricesAsync(
        IEnumerable<TypeId> typeIds,
        RegionId regionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(typeIds);

        List<TypeId> distinctTypeIds = typeIds.Distinct().ToList();
        if (distinctTypeIds.Count == 0)
        {
            return Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Success(new Dictionary<TypeId, MarketPrice>());
        }

        Dictionary<TypeId, MarketPrice> prices = [];

        foreach (TypeId typeId in distinctTypeIds)
        {
            Result<IReadOnlyList<TranquilityOrder>> orders = await GetOrdersAsync(typeId, regionId, cancellationToken).ConfigureAwait(false);
            if (orders.IsFailure)
            {
                return Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Failure(orders.Error);
            }

            Result<IReadOnlyList<TranquilityHistoryEntry>> history = await GetHistoryAsync(typeId, regionId, cancellationToken).ConfigureAwait(false);
            if (history.IsFailure)
            {
                return Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Failure(history.Error);
            }

            prices[typeId] = new MarketPrice(
                typeId,
                orders.Value.Where(order => !order.IsBuyOrder).Select(order => (double?)order.Price).Min(),
                orders.Value.Where(order => order.IsBuyOrder).Select(order => (double?)order.Price).Max(),
                ComputeWeightedAverage(history.Value));
        }

        return Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Success(prices);
    }

    private async Task<Result<IReadOnlyList<TranquilityOrder>>> GetOrdersAsync(TypeId typeId, RegionId regionId, CancellationToken cancellationToken)
    {
        List<TranquilityOrder> orders = [];
        int page = 1;

        while (true)
        {
            string requestUri = $"markets/{regionId.Value}/orders/?datasource=tranquility&type_id={typeId.Value}&order_type=all&page={page}";
            Result<(IReadOnlyList<TranquilityOrder> Payload, int TotalPages)> pageResult = await GetAsync<IReadOnlyList<TranquilityOrder>>(
                requestUri,
                "TRANQUILITY_ORDERS",
                response => ParsePages(response.Headers.TryGetValues("X-Pages", out IEnumerable<string>? values) ? values.FirstOrDefault() : null),
                cancellationToken).ConfigureAwait(false);

            if (pageResult.IsFailure)
            {
                return Result<IReadOnlyList<TranquilityOrder>>.Failure(pageResult.Error);
            }

            orders.AddRange(pageResult.Value.Payload);
            if (page >= pageResult.Value.TotalPages)
            {
                break;
            }

            page++;
        }

        return Result<IReadOnlyList<TranquilityOrder>>.Success(orders);
    }

    private async Task<Result<IReadOnlyList<TranquilityHistoryEntry>>> GetHistoryAsync(TypeId typeId, RegionId regionId, CancellationToken cancellationToken)
    {
        string requestUri = $"markets/{regionId.Value}/history/?datasource=tranquility&type_id={typeId.Value}";
        Result<(IReadOnlyList<TranquilityHistoryEntry> Payload, int TotalPages)> result = await GetAsync<IReadOnlyList<TranquilityHistoryEntry>>(
            requestUri,
            "TRANQUILITY_HISTORY",
            _ => 1,
            cancellationToken).ConfigureAwait(false);

        return result.IsFailure
            ? Result<IReadOnlyList<TranquilityHistoryEntry>>.Failure(result.Error)
            : Result<IReadOnlyList<TranquilityHistoryEntry>>.Success(result.Value.Payload);
    }

    private async Task<Result<(T Payload, int TotalPages)>> GetAsync<T>(
        string requestUri,
        string errorPrefix,
        Func<HttpResponseMessage, int> totalPagesFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                string message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return Result<(T Payload, int TotalPages)>.Failure(
                    $"{errorPrefix}_{(int)response.StatusCode}",
                    string.IsNullOrWhiteSpace(message)
                        ? $"Tranquility market request failed with status code {(int)response.StatusCode}."
                        : message);
            }

            T? payload = await response.Content.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken).ConfigureAwait(false);
            if (payload is null)
            {
                return Result<(T Payload, int TotalPages)>.Failure($"{errorPrefix}_EMPTY_RESPONSE", "Tranquility market request returned an empty response body.");
            }

            return Result<(T Payload, int TotalPages)>.Success((payload, totalPagesFactory(response)));
        }
        catch (HttpRequestException ex)
        {
            return Result<(T Payload, int TotalPages)>.Failure($"{errorPrefix}_HTTP_ERROR", ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<(T Payload, int TotalPages)>.Failure($"{errorPrefix}_TIMEOUT", ex.Message);
        }
        catch (JsonException ex)
        {
            return Result<(T Payload, int TotalPages)>.Failure($"{errorPrefix}_INVALID_JSON", ex.Message);
        }
    }

    private static int ParsePages(string? pageHeaderValue) =>
        int.TryParse(pageHeaderValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pages) && pages > 0 ? pages : 1;

    private static double? ComputeWeightedAverage(IEnumerable<TranquilityHistoryEntry> history)
    {
        double totalVolume = 0;
        double weightedSum = 0;

        foreach (TranquilityHistoryEntry entry in history)
        {
            if (entry.Volume <= 0 || entry.Average <= 0)
            {
                continue;
            }

            totalVolume += entry.Volume;
            weightedSum += entry.Average * entry.Volume;
        }

        return totalVolume > 0 ? weightedSum / totalVolume : null;
    }

    private sealed record TranquilityOrder(
        [property: JsonPropertyName("is_buy_order")] bool IsBuyOrder,
        [property: JsonPropertyName("price")] double Price);

    private sealed record TranquilityHistoryEntry(
        [property: JsonPropertyName("average")] double Average,
        [property: JsonPropertyName("volume")] long Volume);
}