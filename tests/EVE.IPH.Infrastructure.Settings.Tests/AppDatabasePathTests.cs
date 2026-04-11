using EVE.IPH.Infrastructure.Settings.Storage;

namespace EVE.IPH.Infrastructure.Settings.Tests;

public sealed class AppDatabasePathTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"eve-iph-settings-tests-{Guid.NewGuid():N}");

    [Fact]
    public void TryGetExistingDatabasePath_ReturnsExecutableFolderDatabaseFirst()
    {
        string executableDirectory = Path.Combine(_tempRoot, "bin");
        string roamingAppDataDirectory = Path.Combine(_tempRoot, "appdata");
        string executableDatabasePath = CreateDatabaseFile(executableDirectory);
        CreateDatabaseFile(Path.Combine(roamingAppDataDirectory, "EVE IPH"));

        string? path = AppDatabasePath.TryGetExistingDatabasePath(executableDirectory, roamingAppDataDirectory);

        path.Should().Be(executableDatabasePath);
    }

    [Fact]
    public void TryGetExistingDatabasePath_ReturnsAppDataDatabaseWhenExecutableDatabaseMissing()
    {
        string executableDirectory = Path.Combine(_tempRoot, "bin");
        string roamingAppDataDirectory = Path.Combine(_tempRoot, "appdata");
        string appDataDatabasePath = CreateDatabaseFile(Path.Combine(roamingAppDataDirectory, "EVE IPH"));

        string? path = AppDatabasePath.TryGetExistingDatabasePath(executableDirectory, roamingAppDataDirectory);

        path.Should().Be(appDataDatabasePath);
    }

    [Fact]
    public void TryGetExistingDatabasePath_ReturnsNullWhenDatabaseDoesNotExist()
    {
        string executableDirectory = Path.Combine(_tempRoot, "bin");
        string roamingAppDataDirectory = Path.Combine(_tempRoot, "appdata");

        string? path = AppDatabasePath.TryGetExistingDatabasePath(executableDirectory, roamingAppDataDirectory);

        path.Should().BeNull();
    }

    [Fact]
    public void GetCanonicalDatabasePath_ReturnsNonEmptyPath()
    {
        string path = AppDatabasePath.GetCanonicalDatabasePath();

        path.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetCanonicalDatabasePath_IsAbsolutePath()
    {
        string path = AppDatabasePath.GetCanonicalDatabasePath();

        Path.IsPathRooted(path).Should().BeTrue();
    }

    [Fact]
    public void GetCanonicalDatabasePath_EndsWithExpectedFileName()
    {
        string path = AppDatabasePath.GetCanonicalDatabasePath();

        path.Should().EndWith(AppDatabasePath.DatabaseFileName);
    }

    [Fact]
    public void GetCanonicalDatabasePath_IsInsideDataDirectory()
    {
        string canonicalPath = AppDatabasePath.GetCanonicalDatabasePath();
        string dataDirectory = PlatformStoragePath.GetDataDirectory();

        canonicalPath.Should().StartWith(dataDirectory);
    }

    [Fact]
    public void GetCanonicalDatabasePath_DoesNotContainExecutableDirectory()
    {
        string canonicalPath = AppDatabasePath.GetCanonicalDatabasePath();
        string executableDirectory = AppContext.BaseDirectory;

        // The canonical path must never point into the install directory so that
        // Velopack can safely replace the application binaries without affecting user data.
        canonicalPath.Should().NotStartWith(executableDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private static string CreateDatabaseFile(string directory)
    {
        Directory.CreateDirectory(directory);

        string path = Path.Combine(directory, AppDatabasePath.DatabaseFileName);
        File.WriteAllText(path, string.Empty);
        return path;
    }
}
