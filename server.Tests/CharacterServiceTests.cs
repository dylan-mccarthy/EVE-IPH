using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using server.Infrastructure;
using server.Services.Characters;
using Xunit;

namespace server.Tests.Services;

public class CharacterServiceTests
{
    [Fact]
    public async Task GetProfileAsync_ReturnsProfile()
    {
        var handler = new StubHandler(_ =>
        {
            const string json = "{\"name\":\"Tester\",\"corporation_id\":123,\"alliance_id\":456,\"security_status\":1.5,\"birthday\":\"2020-01-01T00:00:00Z\"}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://esi.test/")
        };
        var options = Options.Create(new EsiOptions { BaseUrl = "https://esi.test/" });
        var service = new CharacterService(client, options);

        var profile = await service.GetProfileAsync(42, "token");

        profile.CharacterId.Should().Be(42);
        profile.Name.Should().Be("Tester");
        profile.CorporationId.Should().Be(123);
        profile.AllianceId.Should().Be(456);
        profile.SecurityStatus.Should().Be(1.5);
        profile.Birthday.Should().Be(DateTime.Parse("2020-01-01T00:00:00Z"));

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("/characters/42/");
        handler.LastRequest!.Headers.Authorization?.Scheme.Should().Be("Bearer");
        handler.LastRequest!.Headers.Authorization?.Parameter.Should().Be("token");
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public HttpRequestMessage? LastRequest { get; private set; }

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_handler(request));
        }
    }
}
