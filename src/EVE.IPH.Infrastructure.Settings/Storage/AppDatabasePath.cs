namespace EVE.IPH.Infrastructure.Settings.Storage;

public static class AppDatabasePath
{
    public const string DatabaseFileName = "EVEIPH DB.sqlite";
    private const string LegacyAppFolderName = "EVE IPH";

    /// <summary>
    /// Returns the canonical path for the application database in the user's app-data
    /// directory. The file may not yet exist on a first run; call
    /// <see cref="GetCanonicalDatabasePath"/> and then create the directory and open a
    /// SQLite connection to initialise the file.
    /// </summary>
    /// <remarks>
    /// The canonical path is always inside the user's app-data folder — never inside the
    /// application install directory. Velopack replaces the install directory during
    /// updates, so storing the database there would cause data loss.
    /// </remarks>
    public static string GetCanonicalDatabasePath()
    {
        string dataDirectory = PlatformStoragePath.GetDataDirectory();
        return Path.Combine(dataDirectory, DatabaseFileName);
    }

    /// <summary>
    /// Searches well-known locations for an existing database file and returns its full
    /// path, or <see langword="null"/> when no database is found. Used to locate databases
    /// created by older versions of the application.
    /// </summary>
    public static string? TryGetExistingDatabasePath()
    {
        string executableDirectory = AppContext.BaseDirectory;
        string roamingAppDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        return TryGetExistingDatabasePath(executableDirectory, roamingAppDataDirectory);
    }

    /// <summary>
    /// Searches well-known locations for an existing database file and returns its full
    /// path, or <see langword="null"/> when no database is found.
    /// </summary>
    public static string? TryGetExistingDatabasePath(string executableDirectory, string roamingAppDataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executableDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(roamingAppDataDirectory);

        string executableCandidate = Path.Combine(executableDirectory, DatabaseFileName);
        if (File.Exists(executableCandidate))
        {
            return executableCandidate;
        }

        string appDataCandidate = Path.Combine(roamingAppDataDirectory, LegacyAppFolderName, DatabaseFileName);
        if (File.Exists(appDataCandidate))
        {
            return appDataCandidate;
        }

        return null;
    }
}