using EVE.IPH.Infrastructure.Settings.Storage;

namespace EVE.IPH.Infrastructure.Settings.Tests;

public sealed class PlatformStoragePathTests
{
    [Fact]
    public void GetSettingsDirectory_ReturnsNonEmptyPath()
    {
        string path = PlatformStoragePath.GetSettingsDirectory();

        path.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetSettingsDirectory_ContainsAppFolderName()
    {
        string path = PlatformStoragePath.GetSettingsDirectory();

        path.Should().Contain("EVE-IPH");
    }

    [Fact]
    public void GetSettingsDirectory_ContainsSettingsSubfolder()
    {
        string path = PlatformStoragePath.GetSettingsDirectory();

        path.Should().EndWith("Settings");
    }

    [Fact]
    public void GetSettingsDirectory_IsAbsolutePath()
    {
        string path = PlatformStoragePath.GetSettingsDirectory();

        Path.IsPathRooted(path).Should().BeTrue();
    }

    [Fact]
    public void GetDataDirectory_ReturnsNonEmptyPath()
    {
        string path = PlatformStoragePath.GetDataDirectory();

        path.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetDataDirectory_ContainsAppFolderName()
    {
        string path = PlatformStoragePath.GetDataDirectory();

        path.Should().Contain("EVE-IPH");
    }

    [Fact]
    public void GetDataDirectory_IsAbsolutePath()
    {
        string path = PlatformStoragePath.GetDataDirectory();

        Path.IsPathRooted(path).Should().BeTrue();
    }

    [Fact]
    public void GetDataDirectory_IsParentOfSettingsDirectory_OnWindowsAndMacOs()
    {
        // On Linux the XDG spec separates data ($XDG_DATA_HOME) from config
        // ($XDG_CONFIG_HOME), so the settings directory is not necessarily inside the
        // data directory. This assertion only applies to Windows and macOS where both
        // paths share the same application-support root.
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS())
            return;

        string dataDir = PlatformStoragePath.GetDataDirectory();
        string settingsDir = PlatformStoragePath.GetSettingsDirectory();

        settingsDir.Should().StartWith(dataDir);
    }
}
