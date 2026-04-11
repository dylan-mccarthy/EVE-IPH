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
    public static async Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        string dbPath = AppDatabasePath.GetCanonicalDatabasePath();
        string dbDirectory = Path.GetDirectoryName(dbPath)
            ?? throw new InvalidOperationException($"Cannot determine directory for database path '{dbPath}'.");
        string settingsDirectory = PlatformStoragePath.GetSettingsDirectory();

        Directory.CreateDirectory(dbDirectory);
        Directory.CreateDirectory(settingsDirectory);

        // SQLite creates the file on first connection; the migration runner then applies
        // all pending scripts (including the initial schema) idempotently.
        IDbConnectionFactory connectionFactory = new SqliteConnectionFactory($"Data Source={dbPath}");
        SqliteMigrationRunner migrationRunner = new(connectionFactory);
        JsonSettingsStore settingsStore = new(settingsDirectory);
        StaticDataBootstrapper staticDataBootstrapper = new(settingsStore, connectionFactory, new HttpClient());
        DummyCharacterBootstrapper dummyCharacterBootstrapper = new(connectionFactory);

        await migrationRunner.RunAsync(cancellationToken).ConfigureAwait(false);
        await staticDataBootstrapper.EnsureStaticDataAsync(cancellationToken).ConfigureAwait(false);
        await dummyCharacterBootstrapper.EnsureDummyCharacterAsync(cancellationToken).ConfigureAwait(false);
    }
}
