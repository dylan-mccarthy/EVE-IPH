using System.Net;
using System.Text;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Infrastructure.ESI.Market;

namespace EVE.IPH.Infrastructure.ESI.Tests.Market;

public sealed class TranquilityMarketPriceSourceTests
{
    [Fact]
    public async Task GetPricesAsync_CombinesOrdersAndHistoryIntoMarketPrice()
    {
        RecordingHandler handler = new(request =>
        {
            string pathAndQuery = request.RequestUri!.PathAndQuery;
            if (pathAndQuery == "/latest/markets/10000002/orders/?datasource=tranquility&type_id=34&order_type=all&page=1")
            {
                HttpResponseMessage response = new(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        [
                          { "is_buy_order": false, "price": 5.3 },
                          { "is_buy_order": true, "price": 4.8 }
                        ]
                        """,
                        Encoding.UTF8,
                        "application/json")
                };
                response.Headers.Add("X-Pages", "2");
                return response;
            }

            if (pathAndQuery == "/latest/markets/10000002/orders/?datasource=tranquility&type_id=34&order_type=all&page=2")
            {
                HttpResponseMessage response = new(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        [
                          { "is_buy_order": false, "price": 5.1 },
                          { "is_buy_order": true, "price": 4.9 }
                        ]
                        """,
                        Encoding.UTF8,
                        "application/json")
                };
                response.Headers.Add("X-Pages", "2");
                return response;
            }

            if (pathAndQuery == "/latest/markets/10000002/history/?datasource=tranquility&type_id=34")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        [
                          { "average": 5.0, "volume": 20 },
                          { "average": 6.0, "volume": 10 }
                        ]
                        """,
                        Encoding.UTF8,
                        "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("unexpected request")
            };
        });

        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://esi.evetech.net/latest/")
        };

        TranquilityMarketPriceSource sut = new(httpClient);

        var result = await sut.GetPricesAsync([new TypeId(34)], new RegionId(10000002));

        result.IsSuccess.Should().BeTrue();
        sut.SourceKind.Should().Be(MarketPriceSourceKind.Tranquility);
        result.Value[new TypeId(34)].MinSell.Should().Be(5.1);
        result.Value[new TypeId(34)].MaxBuy.Should().Be(4.9);
        result.Value[new TypeId(34)].Average.Should().BeApproximately(5.3333333333d, 0.0001d);
    }
}