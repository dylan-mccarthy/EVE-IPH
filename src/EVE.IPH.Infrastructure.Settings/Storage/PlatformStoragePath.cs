namespace EVE.IPH.Infrastructure.Settings.Storage;

public static class PlatformStoragePath
{
    private const string AppFolderName = "EVE-IPH";

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
