using System.Globalization;
using System.Net;

namespace EVE.IPH.Infrastructure.ESI.Authentication;

/// <summary>
/// Applies simple retry and ESI error-limit handling for HTTP requests.
/// </summary>
public sealed class EsiResilienceHandler : DelegatingHandler
{
    private const string ErrorLimitRemainHeader = "X-Esi-Error-Limit-Remain";
    private const string ErrorLimitResetHeader = "X-Esi-Error-Limit-Reset";

    private readonly object _syncRoot = new();
    private readonly TimeProvider _timeProvider;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly int _maxRetries;
    private DateTimeOffset _blockedUntilUtc = DateTimeOffset.MinValue;

    public EsiResilienceHandler(TimeProvider timeProvider)
        : this(timeProvider, maxRetries: 3, delayAsync: null)
    {
    }

    public EsiResilienceHandler(
        TimeProvider timeProvider,
        int maxRetries,
        Func<TimeSpan, CancellationToken, Task>? delayAsync)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (maxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries));
        }

        _timeProvider = timeProvider;
        _maxRetries = maxRetries;
        _delayAsync = delayAsync ?? ((delay, cancellationToken) => Task.Delay(delay, _timeProvider, cancellationToken));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        for (int attempt = 0; ; attempt++)
        {
            await DelayIfBlockedAsync(cancellationToken).ConfigureAwait(false);

            HttpRequestMessage attemptRequest = await request.CloneAsync(cancellationToken).ConfigureAwait(false);
            HttpResponseMessage response = await base.SendAsync(attemptRequest, cancellationToken).ConfigureAwait(false);

            UpdateErrorLimitWindow(response);

            if (!ShouldRetry(response, attempt, out TimeSpan delay))
            {
                return response;
            }

            response.Dispose();
            await _delayAsync(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DelayIfBlockedAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset blockedUntilUtc;
        lock (_syncRoot)
        {
            blockedUntilUtc = _blockedUntilUtc;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (blockedUntilUtc > now)
        {
            await _delayAsync(blockedUntilUtc - now, cancellationToken).ConfigureAwait(false);
        }
    }

    private void UpdateErrorLimitWindow(HttpResponseMessage response)
    {
        if (!TryReadIntHeader(response, ErrorLimitRemainHeader, out int remain) || remain > 0)
        {
            return;
        }

        if (!TryReadIntHeader(response, ErrorLimitResetHeader, out int resetSeconds) || resetSeconds <= 0)
        {
            return;
        }

        DateTimeOffset candidateBlockedUntil = _timeProvider.GetUtcNow().AddSeconds(resetSeconds);

        lock (_syncRoot)
        {
            if (candidateBlockedUntil > _blockedUntilUtc)
            {
                _blockedUntilUtc = candidateBlockedUntil;
            }
        }
    }

    private bool ShouldRetry(HttpResponseMessage response, int attempt, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;
        if (attempt >= _maxRetries)
        {
            return false;
        }

        int statusCode = (int)response.StatusCode;
        bool transientFailure = response.StatusCode == HttpStatusCode.TooManyRequests || statusCode >= 500;
        if (!transientFailure)
        {
            return false;
        }

        delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt));
        return true;
    }

    private static bool TryReadIntHeader(HttpResponseMessage response, string headerName, out int value)
    {
        value = 0;
        if (!response.Headers.TryGetValues(headerName, out IEnumerable<string>? values))
        {
            return false;
        }

        string? rawValue = values.FirstOrDefault();
        return int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}