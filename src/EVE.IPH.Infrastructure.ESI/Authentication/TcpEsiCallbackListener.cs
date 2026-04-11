using System.Net;
using System.Net.Sockets;
using System.Text;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Interfaces;

namespace EVE.IPH.Infrastructure.ESI.Authentication;

/// <summary>
/// Receives the browser redirect on the localhost callback endpoint configured for EVE SSO.
/// </summary>
public sealed class TcpEsiCallbackListener(EsiSsoOptions options) : IEsiCallbackListener
{
    private readonly EsiSsoOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    public async Task<Result<EsiAuthorizationCallback>> WaitForCallbackAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Uri redirectUri = new(_options.RedirectUri);
            IPAddress ipAddress = ResolveIpAddress(redirectUri.Host);
            int port = redirectUri.Port;

            using TcpListener listener = new(ipAddress, port);
            listener.Start();

            using TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            await using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            using StreamWriter writer = new(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };

            string? requestLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(requestLine))
            {
                await WriteResponseAsync(writer, "Authorization failed. You can close this window.", cancellationToken).ConfigureAwait(false);
                return Result<EsiAuthorizationCallback>.Failure("ESI_CALLBACK_INVALID_REQUEST", "The callback request was empty.");
            }

            while (!string.IsNullOrEmpty(await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)))
            {
            }

            Result<EsiAuthorizationCallback> callbackResult = ParseRequestLine(requestLine);
            string responseText = callbackResult.IsSuccess && callbackResult.Value.HasAuthorizationCode
                ? "Authorization successful. You can close this window."
                : "Authorization failed. You can close this window.";

            await WriteResponseAsync(writer, responseText, cancellationToken).ConfigureAwait(false);
            return callbackResult;
        }
        catch (OperationCanceledException)
        {
            return Result<EsiAuthorizationCallback>.Failure("ESI_CALLBACK_CANCELLED", "The callback listener was cancelled.");
        }
        catch (Exception ex) when (ex is IOException or SocketException or InvalidOperationException)
        {
            return Result<EsiAuthorizationCallback>.Failure("ESI_CALLBACK_FAILED", ex.Message);
        }
    }

    public static Result<EsiAuthorizationCallback> ParseRequestLine(string requestLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestLine);

        string[] parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || !parts[0].Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            return Result<EsiAuthorizationCallback>.Failure("ESI_CALLBACK_INVALID_REQUEST", "The callback used an unsupported request format.");
        }

        string relativeTarget = parts[1];
        Uri callbackUri = relativeTarget.StartsWith("/", StringComparison.Ordinal)
            ? new Uri($"http://127.0.0.1{relativeTarget}")
            : new Uri($"http://127.0.0.1/{relativeTarget}");

        Dictionary<string, string?> query = ParseQuery(callbackUri.Query);
        if (query.TryGetValue("error", out string? error) && !string.IsNullOrWhiteSpace(error))
        {
            return Result<EsiAuthorizationCallback>.Success(new EsiAuthorizationCallback(
                query.GetValueOrDefault("code"),
                query.GetValueOrDefault("state"),
                error,
                query.GetValueOrDefault("error_description")));
        }

        if (!query.TryGetValue("code", out string? code) || string.IsNullOrWhiteSpace(code))
        {
            return Result<EsiAuthorizationCallback>.Failure("ESI_CALLBACK_CODE_MISSING", "The callback did not include an authorization code.");
        }

        return Result<EsiAuthorizationCallback>.Success(new EsiAuthorizationCallback(
            code,
            query.GetValueOrDefault("state"),
            null,
            null));
    }

    private static async Task WriteResponseAsync(StreamWriter writer, string message, CancellationToken cancellationToken)
    {
        string body = $"<html><body><p>{WebUtility.HtmlEncode(message)}</p></body></html>";
        await writer.WriteLineAsync("HTTP/1.1 200 OK").ConfigureAwait(false);
        await writer.WriteLineAsync("Content-Type: text/html; charset=utf-8").ConfigureAwait(false);
        await writer.WriteLineAsync($"Content-Length: {Encoding.UTF8.GetByteCount(body)}").ConfigureAwait(false);
        await writer.WriteLineAsync(string.Empty).ConfigureAwait(false);
        await writer.WriteAsync(body.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    private static Dictionary<string, string?> ParseQuery(string query)
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal);
        string trimmed = query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return values;
        }

        foreach (string segment in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pair = segment.Split('=', 2);
            string key = WebUtility.UrlDecode(pair[0]);
            string? value = pair.Length > 1 ? WebUtility.UrlDecode(pair[1]) : null;
            values[key] = value;
        }

        return values;
    }

    private static IPAddress ResolveIpAddress(string host)
    {
        if (IPAddress.TryParse(host, out IPAddress? ipAddress))
        {
            return ipAddress;
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return IPAddress.Loopback;
        }

        throw new InvalidOperationException($"Unsupported callback host '{host}'.");
    }
}