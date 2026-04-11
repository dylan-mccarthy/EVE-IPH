using System.Text.Json;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Infrastructure.Settings;

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _settingsDirectory;

    public JsonSettingsStore(string settingsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsDirectory);
        _settingsDirectory = settingsDirectory;
    }

    private string GetFilePath<T>() =>
        Path.Combine(_settingsDirectory, $"{typeof(T).Name}.settings");

    public async Task<Maybe<T>> ReadAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        string filePath = GetFilePath<T>();
        if (!File.Exists(filePath))
        {
            return Maybe<T>.None;
        }

        try
        {
            await using FileStream stream = File.OpenRead(filePath);
            T? result = await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken);
            return result is null ? Maybe<T>.None : Maybe<T>.Some(result);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return Maybe<T>.None;
        }
    }

    public async Task<Result<bool>> WriteAsync<T>(T settings, CancellationToken cancellationToken = default) where T : class
    {
        string filePath = GetFilePath<T>();
        string tempPath = filePath + ".tmp";

        try
        {
            Directory.CreateDirectory(_settingsDirectory);

            await using (FileStream stream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken);
            }

            File.Move(tempPath, filePath, overwrite: true);
            return Result<bool>.Success(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
            return Result<bool>.Failure(new Error("settings.write_failed", ex.Message));
        }
    }
}
