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