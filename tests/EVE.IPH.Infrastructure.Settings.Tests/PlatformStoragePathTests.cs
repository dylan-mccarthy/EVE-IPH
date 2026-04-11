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
}
