namespace EVE.IPH.Infrastructure.Settings.Storage;

public sealed record LegacyDatabaseImportResult(
    string DestinationPath,
    string? BackupPath);