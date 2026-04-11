namespace EVE.IPH.UI.Avalonia.Services;

public interface ILegacyDatabaseImportService
{
    string? GetDetectedLegacyDatabasePath();

    bool ImportWouldOverwrite(string sourcePath);

    Task<LegacyDatabaseImportScreenResult> ImportAsync(string sourcePath, CancellationToken cancellationToken = default);

    Task<LegacyDatabaseImportScreenResult> ImportDetectedAsync(CancellationToken cancellationToken = default);
}