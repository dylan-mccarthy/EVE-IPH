using EVE.IPH.Infrastructure.Data.Connections;
using EVE.IPH.Infrastructure.Data.Migrations;
using EVE.IPH.Infrastructure.Settings;
using EVE.IPH.Infrastructure.Settings.Storage;

namespace EVE.IPH.UI.Avalonia.Startup;

/// <summary>
/// Runs all pre-UI startup steps in order before the Avalonia main loop starts:
/// <list type="number">
///   <item>Ensures the user's app-data directory exists.</item>
///   <item>Opens the SQLite database (creating it on first run).</item>
///   <item>Runs any pending schema migrations.</item>
///   <item>Ensures the configured core static-data snapshot has been imported from the JSONL SDE archive.</item>
/// </list>
/// </summary>
internal static class StartupOrchestrator
{
    /// <summary>
    /// Prepares the application for use. Must be awaited before
    /// <c>BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)</c> is called so that
    /// the database and schema are ready before services are resolved.
    /// </summary>
    public static async Task PrepareAsync(Action<string>? reportProgress = null, CancellationToken cancellationToken = default)
    {
        string dbPath = AppDatabasePath.GetCanonicalDatabasePath();
        string dbDirectory = Path.GetDirectoryName(dbPath)
            ?? throw new InvalidOperationException($"Cannot determine directory for database path '{dbPath}'.");
        string settingsDirectory = PlatformStoragePath.GetSettingsDirectory();

        reportProgress?.Invoke($"Using database at '{dbPath}'.");
        Directory.CreateDirectory(dbDirectory);
        Directory.CreateDirectory(settingsDirectory);
        reportProgress?.Invoke("Ensured application data directories exist.");

        SqliteDatabaseRecoveryResult recoveryResult = SqliteDatabaseRecovery.EnsureUsableDatabaseFile(dbPath);
        if (recoveryResult.InvalidFileWasQuarantined)
        {
            Console.Error.WriteLine(recoveryResult.RestoredFromBackup
                ? $"Recovered invalid SQLite database file '{dbPath}' by quarantining '{recoveryResult.QuarantinedPath}' and restoring backup '{recoveryResult.RestoredBackupPath}'."
                : $"Recovered invalid SQLite database file '{dbPath}' by quarantining '{recoveryResult.QuarantinedPath}'. A fresh database will be created on startup.");
        }
        else
        {
            reportProgress?.Invoke("Validated active SQLite database file.");
        }

        // SQLite creates the file on first connection; the migration runner then applies
        // all pending scripts (including the initial schema) idempotently.
        IDbConnectionFactory connectionFactory = new SqliteConnectionFactory($"Data Source={dbPath}");
        SqliteMigrationRunner migrationRunner = new(connectionFactory);
        JsonSettingsStore settingsStore = new(settingsDirectory);
        StaticDataBootstrapper staticDataBootstrapper = new(settingsStore, connectionFactory, new HttpClient());
        DummyCharacterBootstrapper dummyCharacterBootstrapper = new(connectionFactory);

        reportProgress?.Invoke("Applying database migrations...");
        await migrationRunner.RunAsync(cancellationToken).ConfigureAwait(false);
        reportProgress?.Invoke("Ensuring static data is available...");
        await staticDataBootstrapper.EnsureStaticDataAsync(reportProgress, cancellationToken).ConfigureAwait(false);
        reportProgress?.Invoke("Ensuring fallback dummy character exists...");
        await dummyCharacterBootstrapper.EnsureDummyCharacterAsync(cancellationToken).ConfigureAwait(false);
        reportProgress?.Invoke("Startup preparation finished.");
    }
}
