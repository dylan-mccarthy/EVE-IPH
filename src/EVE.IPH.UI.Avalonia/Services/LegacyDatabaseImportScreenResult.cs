namespace EVE.IPH.UI.Avalonia.Services;

public sealed record LegacyDatabaseImportScreenResult(
    string SourcePath,
    string DestinationPath,
    string? BackupPath);