using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Infrastructure.ESI.Market;

public sealed class EveMarketerMarketPriceSource(HttpClient httpClient) : IMarketPriceSource
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public MarketPriceSourceKind SourceKind => MarketPriceSourceKind.EveMarketer;

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
            string requestUri = $"ec/marketstat/json?typeid={string.Join(',', batch.Select(typeId => typeId.Value))}&regionlimit={regionId.Value}";
            Result<IReadOnlyList<EveMarketerResponse>> response = await GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            if (response.IsFailure)
            {
                return Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Failure(response.Error);
            }

            foreach (EveMarketerResponse item in response.Value)
            {
                long typeIdValue = item.Buy.ForQuery.Types.FirstOrDefault();
                if (typeIdValue == 0)
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

    private async Task<Result<IReadOnlyList<EveMarketerResponse>>> GetAsync(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                string message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return Result<IReadOnlyList<EveMarketerResponse>>.Failure(
                    $"EVEMARKETER_{(int)response.StatusCode}",
                    string.IsNullOrWhiteSpace(message)
                        ? $"EVEMarketer request failed with status code {(int)response.StatusCode}."
                        : message);
            }

            IReadOnlyList<EveMarketerResponse>? payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<EveMarketerResponse>>(SerializerOptions, cancellationToken).ConfigureAwait(false);
            return payload is null
                ? Result<IReadOnlyList<EveMarketerResponse>>.Failure("EVEMARKETER_EMPTY_RESPONSE", "EVEMarketer returned an empty response body.")
                : Result<IReadOnlyList<EveMarketerResponse>>.Success(payload);
        }
        catch (HttpRequestException ex)
        {
            return Result<IReadOnlyList<EveMarketerResponse>>.Failure("EVEMARKETER_HTTP_ERROR", ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyList<EveMarketerResponse>>.Failure("EVEMARKETER_TIMEOUT", ex.Message);
        }
        catch (JsonException ex)
        {
            return Result<IReadOnlyList<EveMarketerResponse>>.Failure("EVEMARKETER_INVALID_JSON", ex.Message);
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

    private sealed record EveMarketerResponse(
        [property: JsonPropertyName("buy")] EveMarketerStat Buy,
        [property: JsonPropertyName("sell")] EveMarketerStat Sell);

    private sealed record EveMarketerStat(
        [property: JsonPropertyName("forQuery")] EveMarketerQuery ForQuery,
        [property: JsonPropertyName("max")] double Max,
        [property: JsonPropertyName("min")] double Min,
        [property: JsonPropertyName("wavg")] double WeightedAverage);

    private sealed record EveMarketerQuery(
        [property: JsonPropertyName("types")] IReadOnlyList<long> Types);
}