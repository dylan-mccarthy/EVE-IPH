using System.Linq;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using server.Infrastructure;
using server.Models;
using server.Services.Blueprints;
using Xunit;

namespace server.Tests.Services;

public class BlueprintServiceTests
{
    [Fact]
    public async Task SearchAsync_FiltersByQueryAndCategory()
    {
        await using var fixture = await BlueprintServiceFixture.CreateAsync();
        var service = fixture.Service;

        var request = new BlueprintSearchRequest("Blueprint", "Ships", "Hull", 1, 10);
        var result = await service.SearchAsync(request);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Name).Should().Contain(new[] { "Caracal Blueprint", "Condor Blueprint" });
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task SearchAsync_ClampsPageAndSize()
    {
        await using var fixture = await BlueprintServiceFixture.CreateAsync();
        var service = fixture.Service;

        var request = new BlueprintSearchRequest(string.Empty, null, null, 0, 500);
        var result = await service.SearchAsync(request);

        result.Total.Should().Be(3);
        result.Page.Should().Be(1); // clamps to 1
        result.PageSize.Should().Be(200); // max size clamp
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchAsync_RespectsOrderingAndPagination()
    {
        await using var fixture = await BlueprintServiceFixture.CreateAsync();
        var service = fixture.Service;

        var request = new BlueprintSearchRequest(string.Empty, null, null, 2, 1);
        var result = await service.SearchAsync(request);

        result.Total.Should().Be(3);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Caracal Blueprint");
    }

    private sealed class BlueprintServiceFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _keeper;
        public BlueprintService Service { get; }

        private BlueprintServiceFixture(SqliteConnection keeper, BlueprintService service)
        {
            _keeper = keeper;
            Service = service;
        }

        public static async Task<BlueprintServiceFixture> CreateAsync()
        {
            var name = Guid.NewGuid().ToString("N");
            var connectionString = $"Data Source=file:{name}?mode=memory&cache=shared";

            var keeper = new SqliteConnection(connectionString);
            await keeper.OpenAsync();
            await InitializeSchemaAsync(keeper);
            await SeedAsync(keeper);

            var factory = new TestConnectionFactory(connectionString);
            var service = new BlueprintService(factory);
            return new BlueprintServiceFixture(keeper, service);
        }

        public ValueTask DisposeAsync()
        {
            return _keeper.DisposeAsync();
        }

        private static async Task InitializeSchemaAsync(SqliteConnection connection)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"CREATE TABLE ALL_BLUEPRINTS (
                BLUEPRINT_ID INTEGER PRIMARY KEY,
                BLUEPRINT_NAME TEXT NOT NULL,
                ITEM_GROUP TEXT NOT NULL,
                ITEM_CATEGORY TEXT NOT NULL
            );";
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task SeedAsync(SqliteConnection connection)
        {
            var insert = connection.CreateCommand();
            insert.CommandText = @"INSERT INTO ALL_BLUEPRINTS (BLUEPRINT_ID, BLUEPRINT_NAME, ITEM_GROUP, ITEM_CATEGORY)
                                   VALUES (@id, @name, @group, @cat);";

            void AddRow(long id, string name, string group, string category)
            {
                insert.Parameters.Clear();
                insert.Parameters.AddWithValue("@id", id);
                insert.Parameters.AddWithValue("@name", name);
                insert.Parameters.AddWithValue("@group", group);
                insert.Parameters.AddWithValue("@cat", category);
                insert.ExecuteNonQuery();
            }

            AddRow(1, "Caracal Blueprint", "Ships", "Hull");
            AddRow(2, "Adaptive Shielding", "Modules", "Defense");
            AddRow(3, "Condor Blueprint", "Ships", "Hull");
            await Task.CompletedTask;
        }
    }

    private sealed class TestConnectionFactory : ISqliteConnectionFactory
    {
        private readonly string _connectionString;

        public TestConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SqliteConnection Create()
        {
            return new SqliteConnection(_connectionString);
        }
    }
}
