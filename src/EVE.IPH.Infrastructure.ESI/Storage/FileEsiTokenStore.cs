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
            PersistedTokenStoreRecord store = await ReadStoreAsync(cancellationToken).ConfigureAwait(false);
            PersistedTokenRecord? persistedToken = store.LastUsedCharacterId.HasValue
                ? store.Tokens.FirstOrDefault(token => token.CharacterId == store.LastUsedCharacterId.Value)
                : store.Tokens.FirstOrDefault();

            return persistedToken is null ? Maybe<EsiTokenRecord>.None : Maybe<EsiTokenRecord>.Some(ToDomainRecord(persistedToken));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException or FormatException)
        {
            return Maybe<EsiTokenRecord>.None;
        }
    }

    public async Task<Maybe<EsiTokenRecord>> ReadAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return Maybe<EsiTokenRecord>.None;
        }

        try
        {
            PersistedTokenStoreRecord store = await ReadStoreAsync(cancellationToken).ConfigureAwait(false);
            PersistedTokenRecord? token = store.Tokens.FirstOrDefault(candidate => candidate.CharacterId == characterId.Value);
            return token is null ? Maybe<EsiTokenRecord>.None : Maybe<EsiTokenRecord>.Some(ToDomainRecord(token));
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

            PersistedTokenStoreRecord store = await ReadStoreSafeAsync(cancellationToken).ConfigureAwait(false);
            List<PersistedTokenRecord> tokens = store.Tokens.ToList();
            PersistedTokenRecord persistedToken = FromDomainRecord(token);

            if (persistedToken.CharacterId.HasValue)
            {
                tokens.RemoveAll(candidate => candidate.CharacterId == persistedToken.CharacterId.Value);
            }
            else
            {
                tokens.RemoveAll(candidate => candidate.CharacterId is null);
            }

            tokens.Add(persistedToken);

            PersistedTokenStoreRecord updatedStore = new(
                persistedToken.CharacterId ?? store.LastUsedCharacterId,
                tokens.OrderBy(candidate => candidate.CharacterId ?? long.MaxValue).ToList());

            string json = JsonSerializer.Serialize(updatedStore, SerializerOptions);
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

    public async Task<Result<bool>> ClearAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return Result<bool>.Success(true);
            }

            PersistedTokenStoreRecord store = await ReadStoreAsync(cancellationToken).ConfigureAwait(false);
            List<PersistedTokenRecord> remainingTokens = store.Tokens
                .Where(token => token.CharacterId != characterId.Value)
                .ToList();

            if (remainingTokens.Count == 0)
            {
                File.Delete(_filePath);
                return Result<bool>.Success(true);
            }

            long? lastUsedCharacterId = store.LastUsedCharacterId == characterId.Value
                ? remainingTokens.FirstOrDefault(token => token.CharacterId.HasValue)?.CharacterId
                : store.LastUsedCharacterId;

            PersistedTokenStoreRecord updatedStore = new(lastUsedCharacterId, remainingTokens);
            string tempPath = _filePath + ".tmp";

            try
            {
                string json = JsonSerializer.Serialize(updatedStore, SerializerOptions);
                PersistedTokenEnvelope envelope = CreateEnvelope(json);

                await using (FileStream stream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await JsonSerializer.SerializeAsync(stream, envelope, SerializerOptions, cancellationToken).ConfigureAwait(false);
                }

                File.Move(tempPath, _filePath, overwrite: true);
                return Result<bool>.Success(true);
            }
            catch
            {
                TryDelete(tempPath);
                throw;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException or FormatException)
        {
            return Result<bool>.Failure("ESI_TOKEN_STORE_CLEAR_FAILED", ex.Message);
        }
    }

    private async Task<PersistedTokenStoreRecord> ReadStoreAsync(CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(_filePath);
        PersistedTokenEnvelope? envelope = await JsonSerializer.DeserializeAsync<PersistedTokenEnvelope>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
        if (envelope is null || string.IsNullOrWhiteSpace(envelope.Payload))
        {
            return new PersistedTokenStoreRecord(null, []);
        }

        string json = envelope.IsProtected
            ? UnprotectPayload(envelope.Payload)
            : Encoding.UTF8.GetString(Convert.FromBase64String(envelope.Payload));

        PersistedTokenStoreRecord? store = JsonSerializer.Deserialize<PersistedTokenStoreRecord>(json, SerializerOptions);
        if (store is not null)
        {
            return store with { Tokens = store.Tokens ?? [] };
        }

        PersistedTokenRecord? legacyToken = JsonSerializer.Deserialize<PersistedTokenRecord>(json, SerializerOptions);
        return legacyToken is null
            ? new PersistedTokenStoreRecord(null, [])
            : new PersistedTokenStoreRecord(legacyToken.CharacterId, [legacyToken]);
    }

    private async Task<PersistedTokenStoreRecord> ReadStoreSafeAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return new PersistedTokenStoreRecord(null, []);
        }

        try
        {
            return await ReadStoreAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException or FormatException)
        {
            return new PersistedTokenStoreRecord(null, []);
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

    private sealed record PersistedTokenStoreRecord(long? LastUsedCharacterId, IReadOnlyList<PersistedTokenRecord> Tokens);

    private sealed record PersistedTokenRecord(
        string AccessToken,
        string RefreshToken,
        DateTimeOffset ExpiresAtUtc,
        IReadOnlyList<string> Scopes,
        long? CharacterId);
}