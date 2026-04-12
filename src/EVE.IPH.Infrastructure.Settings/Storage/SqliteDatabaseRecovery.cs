namespace EVE.IPH.Infrastructure.Settings.Storage;

public static class SqliteDatabaseRecovery
{
    private static readonly byte[] SqliteHeader = "SQLite format 3\0"u8.ToArray();

    public static SqliteDatabaseRecoveryResult EnsureUsableDatabaseFile(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        string resolvedPath = Path.GetFullPath(databasePath);
        if (!File.Exists(resolvedPath))
        {
            return new SqliteDatabaseRecoveryResult(false, false, null, null);
        }

        if (LooksLikeSqliteDatabase(resolvedPath))
        {
            return new SqliteDatabaseRecoveryResult(false, false, null, null);
        }

        string quarantinedPath = BuildQuarantinePath(resolvedPath);
        File.Move(resolvedPath, quarantinedPath);

        string? restoredBackupPath = FindLatestValidBackup(resolvedPath);
        if (restoredBackupPath is not null)
        {
            File.Copy(restoredBackupPath, resolvedPath, overwrite: false);
            return new SqliteDatabaseRecoveryResult(true, true, quarantinedPath, restoredBackupPath);
        }

        return new SqliteDatabaseRecoveryResult(true, false, quarantinedPath, null);
    }

    private static string? FindLatestValidBackup(string databasePath)
    {
        string databaseFileName = Path.GetFileName(databasePath);
        string directory = Path.GetDirectoryName(databasePath)
            ?? throw new InvalidOperationException($"Cannot determine directory for database path '{databasePath}'.");

        return Directory.EnumerateFiles(directory, databaseFileName + ".backup-*")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault(LooksLikeSqliteDatabase);
    }

    private static bool LooksLikeSqliteDatabase(string path)
    {
        FileInfo file = new(path);
        if (!file.Exists || file.Length < SqliteHeader.Length)
        {
            return false;
        }

        using FileStream stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[SqliteHeader.Length];
        int bytesRead = stream.Read(header);
        return bytesRead == SqliteHeader.Length && header.SequenceEqual(SqliteHeader);
    }

    private static string BuildQuarantinePath(string databasePath)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string candidatePath = databasePath + ".invalid-" + timestamp;
        int counter = 1;

        while (File.Exists(candidatePath))
        {
            candidatePath = databasePath + ".invalid-" + timestamp + "-" + counter;
            counter++;
        }

        return candidatePath;
    }
}

public sealed record SqliteDatabaseRecoveryResult(
    bool InvalidFileWasQuarantined,
    bool RestoredFromBackup,
    string? QuarantinedPath,
    string? RestoredBackupPath);