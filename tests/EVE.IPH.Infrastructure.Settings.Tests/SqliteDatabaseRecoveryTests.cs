using System.Text;
using EVE.IPH.Infrastructure.Settings.Storage;

namespace EVE.IPH.Infrastructure.Settings.Tests;

public sealed class SqliteDatabaseRecoveryTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"eve-iph-sqlite-recovery-tests-{Guid.NewGuid():N}");

    [Fact]
    public void EnsureUsableDatabaseFile_WhenDatabaseDoesNotExist_DoesNothing()
    {
        string databasePath = Path.Combine(_tempRoot, AppDatabasePath.DatabaseFileName);

        SqliteDatabaseRecoveryResult result = SqliteDatabaseRecovery.EnsureUsableDatabaseFile(databasePath);

        result.InvalidFileWasQuarantined.Should().BeFalse();
        result.RestoredFromBackup.Should().BeFalse();
        File.Exists(databasePath).Should().BeFalse();
    }

    [Fact]
    public void EnsureUsableDatabaseFile_WhenActiveFileIsInvalidAndBackupIsValid_RestoresBackup()
    {
        string databasePath = CreateFile(_tempRoot, AppDatabasePath.DatabaseFileName, "legacy-db");
        string backupPath = CreateFile(_tempRoot, AppDatabasePath.DatabaseFileName + ".backup-20260412000101", CreateSqliteLikeBytes("backup-db"));

        SqliteDatabaseRecoveryResult result = SqliteDatabaseRecovery.EnsureUsableDatabaseFile(databasePath);

        result.InvalidFileWasQuarantined.Should().BeTrue();
        result.RestoredFromBackup.Should().BeTrue();
        result.QuarantinedPath.Should().NotBeNull();
        result.RestoredBackupPath.Should().Be(backupPath);
        File.Exists(result.QuarantinedPath!).Should().BeTrue();
        File.ReadAllText(result.QuarantinedPath!).Should().Be("legacy-db");
        File.ReadAllBytes(databasePath).Should().Equal(File.ReadAllBytes(backupPath));
    }

    [Fact]
    public void EnsureUsableDatabaseFile_WhenActiveFileIsInvalidAndNoValidBackupExists_QuarantinesFile()
    {
        string databasePath = CreateFile(_tempRoot, AppDatabasePath.DatabaseFileName, "legacy-db");
        CreateFile(_tempRoot, AppDatabasePath.DatabaseFileName + ".backup-20260412000101", "still-not-sqlite");

        SqliteDatabaseRecoveryResult result = SqliteDatabaseRecovery.EnsureUsableDatabaseFile(databasePath);

        result.InvalidFileWasQuarantined.Should().BeTrue();
        result.RestoredFromBackup.Should().BeFalse();
        result.QuarantinedPath.Should().NotBeNull();
        result.RestoredBackupPath.Should().BeNull();
        File.Exists(databasePath).Should().BeFalse();
        File.ReadAllText(result.QuarantinedPath!).Should().Be("legacy-db");
    }

    [Fact]
    public void EnsureUsableDatabaseFile_WhenActiveFileAlreadyLooksLikeSqlite_LeavesItInPlace()
    {
        string databasePath = CreateFile(_tempRoot, AppDatabasePath.DatabaseFileName, CreateSqliteLikeBytes("current-db"));

        SqliteDatabaseRecoveryResult result = SqliteDatabaseRecovery.EnsureUsableDatabaseFile(databasePath);

        result.InvalidFileWasQuarantined.Should().BeFalse();
        result.RestoredFromBackup.Should().BeFalse();
        result.QuarantinedPath.Should().BeNull();
        File.Exists(databasePath).Should().BeTrue();
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
        return CreateFile(directory, fileName, Encoding.UTF8.GetBytes(content));
    }

    private static string CreateFile(string directory, string fileName, byte[] content)
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, fileName);
        File.WriteAllBytes(path, content);
        return path;
    }

    private static byte[] CreateSqliteLikeBytes(string payload)
    {
        byte[] header = Encoding.ASCII.GetBytes("SQLite format 3\0");
        byte[] body = Encoding.UTF8.GetBytes(payload);
        byte[] bytes = new byte[header.Length + body.Length];
        Buffer.BlockCopy(header, 0, bytes, 0, header.Length);
        Buffer.BlockCopy(body, 0, bytes, header.Length, body.Length);
        return bytes;
    }
}