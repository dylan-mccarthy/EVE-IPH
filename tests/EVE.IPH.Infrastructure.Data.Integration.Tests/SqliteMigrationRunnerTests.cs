using Dapper;
using EVE.IPH.Infrastructure.Data.Connections;
using EVE.IPH.Infrastructure.Data.Migrations;
using Microsoft.Data.Sqlite;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteMigrationRunnerTests : IDisposable
{
    private readonly SqliteConnection _keeper;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IMigrationRunner _sut;

    public SqliteMigrationRunnerTests()
    {
        string dbName = $"eveiph_migration_test_{Guid.NewGuid():N}";
        string connectionString = $"Data Source=file:{dbName}?mode=memory&cache=shared";

        _connectionFactory = new SqliteConnectionFactory(connectionString);

        _keeper = new SqliteConnection(connectionString);
        _keeper.Open();

        _sut = new SqliteMigrationRunner(_connectionFactory);
    }

    public void Dispose()
    {
        _keeper.Close();
        _keeper.Dispose();
    }

    [Fact]
    public async Task RunAsync_FirstRun_CreatesMigrationsTable()
    {
        await _sut.RunAsync();

        long count = _keeper.ExecuteScalar<long>("SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name='SCHEMA_MIGRATIONS'");
        count.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_FirstRun_CreatesShoppingListItemsTable()
    {
        await _sut.RunAsync();

        long count = _keeper.ExecuteScalar<long>("SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name='SHOPPING_LIST_ITEMS'");
        count.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_SecondRun_IsIdempotent()
    {
        await _sut.RunAsync();
        Func<Task> secondRun = async () => await _sut.RunAsync();

        await secondRun.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RunAsync_RecordsMigrationVersions()
    {
        await _sut.RunAsync();

        long versionCount = _keeper.ExecuteScalar<long>("SELECT COUNT(1) FROM SCHEMA_MIGRATIONS");
        versionCount.Should().BeGreaterThan(0);
    }
}
