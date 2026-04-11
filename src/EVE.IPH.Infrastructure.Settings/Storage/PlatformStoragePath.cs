namespace EVE.IPH.Infrastructure.Settings.Storage;

public static class PlatformStoragePath
{
    private const string AppFolderName = "EVE-IPH";

    /// <summary>
    /// Returns the platform-appropriate directory for the application's user data files
    /// (database, etc.). This directory is outside the install directory so Velopack
    /// can safely replace application binaries without affecting user data.
    /// </summary>
    public static string GetDataDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, AppFolderName);
        }

        if (OperatingSystem.IsMacOS())
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", AppFolderName);
        }

        // Linux and other Unix — honour XDG base directory spec
        string dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        return Path.Combine(dataHome, AppFolderName);
    }

    /// <summary>
    /// Returns the platform-appropriate directory for the application's settings files.
    /// </summary>
    public static string GetSettingsDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, AppFolderName, "Settings");
        }

        if (OperatingSystem.IsMacOS())
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", AppFolderName, "Settings");
        }

        // Linux and other Unix — honour XDG base directory spec
        string configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        return Path.Combine(configHome, AppFolderName, "Settings");
    }
}
