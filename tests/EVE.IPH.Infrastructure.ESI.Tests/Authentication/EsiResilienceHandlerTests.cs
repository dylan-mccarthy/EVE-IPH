using System.Net;
using EVE.IPH.Infrastructure.ESI.Authentication;

namespace EVE.IPH.Infrastructure.ESI.Tests.Authentication;

public sealed class EsiResilienceHandlerTests
{
    [Fact]
    public async Task SendAsync_OnTransientFailure_RetriesWithBackoff()
    {
        List<TimeSpan> delays = [];
        Queue<HttpResponseMessage> responses = new([
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            new HttpResponseMessage(HttpStatusCode.OK)
        ]);

        RecordingHandler innerHandler = new(_ => responses.Dequeue());
        EsiResilienceHandler handler = new(new FakeTimeProvider(), 3, (delay, _) =>
        {
            delays.Add(delay);
            return Task.CompletedTask;
        })
        {
            InnerHandler = innerHandler
        };

        using HttpMessageInvoker invoker = new(handler);

        using HttpResponseMessage response = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://esi.evetech.net/latest/characters/1/"),
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        innerHandler.Requests.Should().HaveCount(2);
        delays.Should().ContainSingle().Which.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SendAsync_WhenErrorLimitIsReached_DelaysNextRequestUntilReset()
    {
        FakeTimeProvider timeProvider = new();
        List<TimeSpan> delays = [];

        HttpResponseMessage firstResponse = new(HttpStatusCode.OK);
        firstResponse.Headers.TryAddWithoutValidation("X-Esi-Error-Limit-Remain", "0");
        firstResponse.Headers.TryAddWithoutValidation("X-Esi-Error-Limit-Reset", "5");

        Queue<HttpResponseMessage> responses = new([
            firstResponse,
            new HttpResponseMessage(HttpStatusCode.OK)
        ]);

        RecordingHandler innerHandler = new(_ => responses.Dequeue());
        EsiResilienceHandler handler = new(timeProvider, 0, (delay, _) =>
        {
            delays.Add(delay);
            timeProvider.Advance(delay);
            return Task.CompletedTask;
        })
        {
            InnerHandler = innerHandler
        };

        using HttpMessageInvoker invoker = new(handler);

        using HttpResponseMessage _ = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://esi.evetech.net/latest/characters/1/"),
            CancellationToken.None);

        using HttpResponseMessage response = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://esi.evetech.net/latest/characters/1/skills/"),
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        delays.Should().ContainSingle().Which.Should().Be(TimeSpan.FromSeconds(5));
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new(2026, 4, 11, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan by) => _utcNow = _utcNow.Add(by);
    }
}