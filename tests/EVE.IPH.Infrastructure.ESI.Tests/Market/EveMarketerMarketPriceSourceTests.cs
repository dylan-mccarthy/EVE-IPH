using System.Net;
using System.Text;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Infrastructure.ESI.Market;

namespace EVE.IPH.Infrastructure.ESI.Tests.Market;

public sealed class EveMarketerMarketPriceSourceTests
{
    [Fact]
    public async Task GetPricesAsync_MapsPayloadToMarketPrices()
    {
        RecordingHandler handler = new(request =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.PathAndQuery.Should().Be("/ec/marketstat/json?typeid=34,35&regionlimit=10000002");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    [
                      {
                        "buy": { "forQuery": { "types": [34] }, "max": 4.7, "min": 4.0, "wavg": 4.3 },
                        "sell": { "forQuery": { "types": [34] }, "max": 5.4, "min": 5.1, "wavg": 5.2 }
                      },
                      {
                        "buy": { "forQuery": { "types": [35] }, "max": 8.2, "min": 7.8, "wavg": 8.0 },
                        "sell": { "forQuery": { "types": [35] }, "max": 9.5, "min": 9.1, "wavg": 9.3 }
                      }
                    ]
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });

        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://api.evemarketer.com/")
        };

        EveMarketerMarketPriceSource sut = new(httpClient);

        var result = await sut.GetPricesAsync([new TypeId(34), new TypeId(35)], new RegionId(10000002));

        result.IsSuccess.Should().BeTrue();
        sut.SourceKind.Should().Be(MarketPriceSourceKind.EveMarketer);
        result.Value[new TypeId(34)].MinSell.Should().Be(5.1);
        result.Value[new TypeId(35)].Average.Should().Be(9.3);
    }
}