using EVE.IPH.Infrastructure.Settings.Storage;

namespace EVE.IPH.Infrastructure.Settings.Tests;

public sealed class LegacyDatabaseImporterTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"eve-iph-import-tests-{Guid.NewGuid():N}");

    [Fact]
    public void Import_CopiesLegacyDatabaseToCanonicalLocation()
    {
        string sourceDirectory = Path.Combine(_tempRoot, "source");
        string sourcePath = CreateFile(sourceDirectory, AppDatabasePath.DatabaseFileName, "legacy-db");
        string destinationPath = Path.Combine(_tempRoot, "appdata", AppDatabasePath.DatabaseFileName);

        LegacyDatabaseImportResult result = LegacyDatabaseImporter.Import(sourcePath, destinationPath);

        File.Exists(result.DestinationPath).Should().BeTrue();
        File.ReadAllText(result.DestinationPath).Should().Be("legacy-db");
        result.BackupPath.Should().BeNull();
    }

    [Fact]
    public void Import_WhenDestinationExists_CreatesBackupBeforeOverwrite()
    {
        string sourcePath = CreateFile(Path.Combine(_tempRoot, "source"), AppDatabasePath.DatabaseFileName, "legacy-db");
        string destinationPath = Path.Combine(_tempRoot, "appdata", AppDatabasePath.DatabaseFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.WriteAllText(destinationPath, "current-db");

        LegacyDatabaseImportResult result = LegacyDatabaseImporter.Import(sourcePath, destinationPath);

        result.BackupPath.Should().NotBeNull();
        File.ReadAllText(result.BackupPath!).Should().Be("current-db");
        File.ReadAllText(result.DestinationPath).Should().Be("legacy-db");
    }

    [Fact]
    public void Import_WhenSourceDoesNotExist_Throws()
    {
        string missingPath = Path.Combine(_tempRoot, "missing", AppDatabasePath.DatabaseFileName);

        Action act = () => LegacyDatabaseImporter.Import(missingPath);

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Import_WhenSourceIsCanonicalPath_Throws()
    {
        string canonicalPath = Path.Combine(_tempRoot, "appdata", AppDatabasePath.DatabaseFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(canonicalPath)!);
        File.WriteAllText(canonicalPath, "current-db");

        Action act = () => LegacyDatabaseImporter.Import(canonicalPath, canonicalPath);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WouldOverwriteExistingDatabase_ReturnsTrue_WhenDifferentDestinationAlreadyExists()
    {
        string sourcePath = CreateFile(Path.Combine(_tempRoot, "source"), AppDatabasePath.DatabaseFileName, "legacy-db");
        string destinationPath = CreateFile(Path.Combine(_tempRoot, "appdata"), AppDatabasePath.DatabaseFileName, "current-db");

        bool result = LegacyDatabaseImporter.WouldOverwriteExistingDatabase(sourcePath, destinationPath);

        result.Should().BeTrue();
    }

    [Fact]
    public void WouldOverwriteExistingDatabase_ReturnsFalse_WhenDestinationDoesNotExist()
    {
        string sourcePath = CreateFile(Path.Combine(_tempRoot, "source"), AppDatabasePath.DatabaseFileName, "legacy-db");
        string destinationPath = Path.Combine(_tempRoot, "appdata", AppDatabasePath.DatabaseFileName);

        bool result = LegacyDatabaseImporter.WouldOverwriteExistingDatabase(sourcePath, destinationPath);

        result.Should().BeFalse();
    }

    [Fact]
    public void WouldOverwriteExistingDatabase_ReturnsFalse_WhenSourceIsDestination()
    {
        string canonicalPath = CreateFile(Path.Combine(_tempRoot, "appdata"), AppDatabasePath.DatabaseFileName, "current-db");

        bool result = LegacyDatabaseImporter.WouldOverwriteExistingDatabase(canonicalPath, canonicalPath);

        result.Should().BeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private static string CreateFile(string directory, string fileName, string content)
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, fileName);
        File.WriteAllText(path, content);
        return path;
    }
}