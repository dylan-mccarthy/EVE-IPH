namespace EVE.IPH.Infrastructure.Settings.Storage;

public static class AppDatabasePath
{
    public const string DatabaseFileName = "EVEIPH DB.sqlite";
    private const string LegacyAppFolderName = "EVE IPH";

    public static string? TryGetExistingDatabasePath()
    {
        string executableDirectory = AppContext.BaseDirectory;
        string roamingAppDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        return TryGetExistingDatabasePath(executableDirectory, roamingAppDataDirectory);
    }

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