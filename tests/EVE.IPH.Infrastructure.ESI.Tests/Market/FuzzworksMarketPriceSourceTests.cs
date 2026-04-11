using System.Net;
using System.Text;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Infrastructure.ESI.Market;

namespace EVE.IPH.Infrastructure.ESI.Tests.Market;

public sealed class FuzzworksMarketPriceSourceTests
{
    [Fact]
    public async Task GetPricesAsync_MapsPayloadToMarketPrices()
    {
        RecordingHandler handler = new(request =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.PathAndQuery.Should().Be("/aggregates/?region=10000002&types=34,35");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "34": {
                        "buy": { "max": 4.7, "min": 4.0, "weightedAverage": 4.3 },
                        "sell": { "max": 5.4, "min": 5.1, "weightedAverage": 5.2 }
                      },
                      "35": {
                        "buy": { "max": 8.2, "min": 7.8, "weightedAverage": 8.0 },
                        "sell": { "max": 9.5, "min": 9.1, "weightedAverage": 9.3 }
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });

        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://market.fuzzwork.co.uk/")
        };

        FuzzworksMarketPriceSource sut = new(httpClient);

        var result = await sut.GetPricesAsync([new TypeId(34), new TypeId(35)], new RegionId(10000002));

        result.IsSuccess.Should().BeTrue();
        sut.SourceKind.Should().Be(MarketPriceSourceKind.Fuzzworks);
        result.Value[new TypeId(34)].MinSell.Should().Be(5.1);
        result.Value[new TypeId(35)].Average.Should().Be(9.3);
    }
}