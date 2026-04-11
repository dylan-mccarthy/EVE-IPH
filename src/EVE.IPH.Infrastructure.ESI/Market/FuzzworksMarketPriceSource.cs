using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Infrastructure.ESI.Market;

public sealed class FuzzworksMarketPriceSource(HttpClient httpClient) : IMarketPriceSource
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public MarketPriceSourceKind SourceKind => MarketPriceSourceKind.Fuzzworks;

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

        foreach (IReadOnlyList<TypeId> batch in Batch(distinctTypeIds, 100))
        {
            string requestUri = $"aggregates/?region={regionId.Value}&types={string.Join(',', batch.Select(typeId => typeId.Value))}";
            Result<Dictionary<string, FuzzworksResponse>> response = await GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            if (response.IsFailure)
            {
                return Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Failure(response.Error);
            }

            foreach ((string key, FuzzworksResponse item) in response.Value)
            {
                if (!long.TryParse(key, out long typeIdValue))
                {
                    continue;
                }

                TypeId typeId = new(typeIdValue);
                prices[typeId] = new MarketPrice(
                    typeId,
                    Normalize(item.Sell.Min),
                    Normalize(item.Buy.Max),
                    Normalize(item.Sell.WeightedAverage));
            }
        }

        return Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Success(prices);
    }

    private async Task<Result<Dictionary<string, FuzzworksResponse>>> GetAsync(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                string message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return Result<Dictionary<string, FuzzworksResponse>>.Failure(
                    $"FUZZWORKS_{(int)response.StatusCode}",
                    string.IsNullOrWhiteSpace(message)
                        ? $"Fuzzworks request failed with status code {(int)response.StatusCode}."
                        : message);
            }

            Dictionary<string, FuzzworksResponse>? payload = await response.Content.ReadFromJsonAsync<Dictionary<string, FuzzworksResponse>>(SerializerOptions, cancellationToken).ConfigureAwait(false);
            return payload is null
                ? Result<Dictionary<string, FuzzworksResponse>>.Failure("FUZZWORKS_EMPTY_RESPONSE", "Fuzzworks returned an empty response body.")
                : Result<Dictionary<string, FuzzworksResponse>>.Success(payload);
        }
        catch (HttpRequestException ex)
        {
            return Result<Dictionary<string, FuzzworksResponse>>.Failure("FUZZWORKS_HTTP_ERROR", ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<Dictionary<string, FuzzworksResponse>>.Failure("FUZZWORKS_TIMEOUT", ex.Message);
        }
        catch (JsonException ex)
        {
            return Result<Dictionary<string, FuzzworksResponse>>.Failure("FUZZWORKS_INVALID_JSON", ex.Message);
        }
    }

    private static double? Normalize(double value) => value > 0 ? value : null;

    private static IEnumerable<IReadOnlyList<TypeId>> Batch(IReadOnlyList<TypeId> typeIds, int batchSize)
    {
        for (int index = 0; index < typeIds.Count; index += batchSize)
        {
            yield return typeIds.Skip(index).Take(batchSize).ToList();
        }
    }

    private sealed record FuzzworksResponse(
        [property: JsonPropertyName("buy")] FuzzworksStat Buy,
        [property: JsonPropertyName("sell")] FuzzworksStat Sell);

    private sealed record FuzzworksStat(
        [property: JsonPropertyName("max")] double Max,
        [property: JsonPropertyName("min")] double Min,
        [property: JsonPropertyName("weightedAverage")] double WeightedAverage);
}