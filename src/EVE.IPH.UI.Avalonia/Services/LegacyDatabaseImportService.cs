using EVE.IPH.Infrastructure.Settings.Storage;
using EVE.IPH.UI.Avalonia.Startup;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class LegacyDatabaseImportService : ILegacyDatabaseImportService
{
    public string? GetDetectedLegacyDatabasePath()
    {
        string? candidatePath = AppDatabasePath.TryGetExistingDatabasePath();
        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            return null;
        }

        string canonicalPath = Path.GetFullPath(AppDatabasePath.GetCanonicalDatabasePath());
        string resolvedCandidatePath = Path.GetFullPath(candidatePath);
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(resolvedCandidatePath, canonicalPath, comparison)
            ? null
            : resolvedCandidatePath;
    }

    public bool ImportWouldOverwrite(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        return LegacyDatabaseImporter.WouldOverwriteExistingDatabase(
            sourcePath,
            AppDatabasePath.GetCanonicalDatabasePath());
    }

    public async Task<LegacyDatabaseImportScreenResult> ImportAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        LegacyDatabaseImportResult result = LegacyDatabaseImporter.Import(sourcePath);
        await StartupOrchestrator.PrepareAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return new LegacyDatabaseImportScreenResult(sourcePath, result.DestinationPath, result.BackupPath);
    }

    public async Task<LegacyDatabaseImportScreenResult> ImportDetectedAsync(CancellationToken cancellationToken = default)
    {
        string? sourcePath = GetDetectedLegacyDatabasePath();
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new InvalidOperationException("No legacy database was detected to import.");
        }

        return await ImportAsync(sourcePath, cancellationToken).ConfigureAwait(false);
    }
}