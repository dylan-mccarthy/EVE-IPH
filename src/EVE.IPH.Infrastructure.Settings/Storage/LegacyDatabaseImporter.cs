namespace EVE.IPH.Infrastructure.Settings.Storage;

public static class LegacyDatabaseImporter
{
    public static LegacyDatabaseImportResult Import(string sourcePath)
    {
        return Import(sourcePath, AppDatabasePath.GetCanonicalDatabasePath());
    }

    public static LegacyDatabaseImportResult Import(string sourcePath, string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        string resolvedSourcePath = Path.GetFullPath(sourcePath);
        if (!File.Exists(resolvedSourcePath))
        {
            throw new FileNotFoundException("The selected legacy database file does not exist.", resolvedSourcePath);
        }

        string resolvedDestinationPath = Path.GetFullPath(destinationPath);
        if (PathsEqual(resolvedSourcePath, resolvedDestinationPath))
        {
            throw new InvalidOperationException("The selected database is already the application's active database.");
        }

        string destinationDirectory = Path.GetDirectoryName(resolvedDestinationPath)
            ?? throw new InvalidOperationException($"Cannot determine directory for database path '{resolvedDestinationPath}'.");

        Directory.CreateDirectory(destinationDirectory);

        string? backupPath = null;
        if (File.Exists(resolvedDestinationPath))
        {
            backupPath = BuildBackupPath(resolvedDestinationPath);
            File.Copy(resolvedDestinationPath, backupPath, overwrite: false);
        }

        File.Copy(resolvedSourcePath, resolvedDestinationPath, overwrite: true);

        return new LegacyDatabaseImportResult(resolvedDestinationPath, backupPath);
    }

    public static bool WouldOverwriteExistingDatabase(string sourcePath, string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        string resolvedSourcePath = Path.GetFullPath(sourcePath);
        string resolvedDestinationPath = Path.GetFullPath(destinationPath);

        if (PathsEqual(resolvedSourcePath, resolvedDestinationPath))
        {
            return false;
        }

        return File.Exists(resolvedDestinationPath);
    }

    private static string BuildBackupPath(string destinationPath)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string candidatePath = destinationPath + ".backup-" + timestamp;
        int counter = 1;

        while (File.Exists(candidatePath))
        {
            candidatePath = destinationPath + ".backup-" + timestamp + "-" + counter;
            counter++;
        }

        return candidatePath;
    }

    private static bool PathsEqual(string leftPath, string rightPath)
    {
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(leftPath, rightPath, comparison);
    }
}