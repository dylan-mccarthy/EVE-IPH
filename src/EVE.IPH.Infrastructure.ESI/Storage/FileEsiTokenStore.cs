using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Infrastructure.ESI.Storage;

/// <summary>
/// Persists ESI tokens to a local file and encrypts them on Windows using DPAPI.
/// </summary>
public sealed class FileEsiTokenStore : IEsiTokenStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _filePath;

    public FileEsiTokenStore()
        : this(Path.Combine(EsiStoragePath.GetAuthDirectory(), "esi-token.json"))
    {
    }

    public FileEsiTokenStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public async Task<Maybe<EsiTokenRecord>> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return Maybe<EsiTokenRecord>.None;
        }

        try
        {
            await using FileStream stream = File.OpenRead(_filePath);
            PersistedTokenEnvelope? envelope = await JsonSerializer.DeserializeAsync<PersistedTokenEnvelope>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
            if (envelope is null || string.IsNullOrWhiteSpace(envelope.Payload))
            {
                return Maybe<EsiTokenRecord>.None;
            }

            string json = envelope.IsProtected
                ? UnprotectPayload(envelope.Payload)
                : Encoding.UTF8.GetString(Convert.FromBase64String(envelope.Payload));

            PersistedTokenRecord? persistedToken = JsonSerializer.Deserialize<PersistedTokenRecord>(json, SerializerOptions);
            if (persistedToken is null)
            {
                return Maybe<EsiTokenRecord>.None;
            }

            return Maybe<EsiTokenRecord>.Some(ToDomainRecord(persistedToken));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException or FormatException)
        {
            return Maybe<EsiTokenRecord>.None;
        }
    }

    public async Task<Result<EsiTokenRecord>> WriteAsync(EsiTokenRecord token, CancellationToken cancellationToken = default)
    {
        string tempPath = _filePath + ".tmp";

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

            string json = JsonSerializer.Serialize(FromDomainRecord(token), SerializerOptions);
            PersistedTokenEnvelope envelope = CreateEnvelope(json);

            await using (FileStream stream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, envelope, SerializerOptions, cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempPath, _filePath, overwrite: true);
            return Result<EsiTokenRecord>.Success(token);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException)
        {
            TryDelete(tempPath);
            return Result<EsiTokenRecord>.Failure("ESI_TOKEN_STORE_WRITE_FAILED", ex.Message);
        }
    }

    public Task<Result<bool>> ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }

            return Task.FromResult(Result<bool>.Success(true));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(Result<bool>.Failure("ESI_TOKEN_STORE_CLEAR_FAILED", ex.Message));
        }
    }

    private static PersistedTokenEnvelope CreateEnvelope(string json)
    {
        byte[] data = Encoding.UTF8.GetBytes(json);
        if (OperatingSystem.IsWindows())
        {
            byte[] protectedBytes = ProtectedData.Protect(data, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
            return new PersistedTokenEnvelope(1, Convert.ToBase64String(protectedBytes), true);
        }

        return new PersistedTokenEnvelope(1, Convert.ToBase64String(data), false);
    }

    private static string UnprotectPayload(string payload)
    {
        byte[] bytes = Convert.FromBase64String(payload);
        if (OperatingSystem.IsWindows())
        {
            byte[] unprotectedBytes = ProtectedData.Unprotect(bytes, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(unprotectedBytes);
        }

        return Encoding.UTF8.GetString(bytes);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }

    private static PersistedTokenRecord FromDomainRecord(EsiTokenRecord token) => new(
        token.AccessToken,
        token.RefreshToken,
        token.ExpiresAtUtc,
        token.Scopes,
        token.CharacterId.HasValue ? token.CharacterId.Value.Value : null);

    private static EsiTokenRecord ToDomainRecord(PersistedTokenRecord token) => new(
        token.AccessToken,
        token.RefreshToken,
        token.ExpiresAtUtc,
        token.Scopes,
        token.CharacterId.HasValue ? Maybe<CharacterId>.Some(new CharacterId(token.CharacterId.Value)) : Maybe<CharacterId>.None);

    private sealed record PersistedTokenEnvelope(int Version, string Payload, bool IsProtected);

    private sealed record PersistedTokenRecord(
        string AccessToken,
        string RefreshToken,
        DateTimeOffset ExpiresAtUtc,
        IReadOnlyList<string> Scopes,
        long? CharacterId);
}