using Dapper;
using EVE.IPH.Domain.Core;
using EVE.IPH.Infrastructure.Data.Connections;
using EVE.IPH.Infrastructure.Data.Migrations;
using EVE.IPH.UI.Avalonia.Startup;

namespace EVE.IPH.UI.Avalonia.Tests.Startup;

public sealed class DummyCharacterBootstrapperTests : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly string _databasePath;

    public DummyCharacterBootstrapperTests()
    {
        _databasePath = Path.Combine(_rootDirectory, "eveiph.sqlite");
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task EnsureDummyCharacterAsync_SeedsPlaceholderCharacterAndAllSkillTypes()
    {
        IDbConnectionFactory connectionFactory = await CreateMigratedDatabaseAsync();
        await SeedSkillTypesAsync(connectionFactory);
        DummyCharacterBootstrapper bootstrapper = new(connectionFactory);

        await bootstrapper.EnsureDummyCharacterAsync();

        using System.Data.IDbConnection connection = connectionFactory.CreateConnection();
        long characterCount = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(1) FROM ESI_CHARACTER_DATA WHERE CHARACTER_ID = @CharacterId",
            new { CharacterId = SpecialCharacters.AllSkillsVId.Value });
        long skillCount = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(1) FROM CHARACTER_SKILLS WHERE CHARACTER_ID = @CharacterId",
            new { CharacterId = SpecialCharacters.AllSkillsVId.Value });
        long maxLevelSkillCount = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(1) FROM CHARACTER_SKILLS WHERE CHARACTER_ID = @CharacterId AND TRAINED_SKILL_LEVEL = 5 AND ACTIVE_SKILL_LEVEL = 5",
            new { CharacterId = SpecialCharacters.AllSkillsVId.Value });
        int isDefault = await connection.ExecuteScalarAsync<int>(
            "SELECT IS_DEFAULT FROM ESI_CHARACTER_DATA WHERE CHARACTER_ID = @CharacterId",
            new { CharacterId = SpecialCharacters.AllSkillsVId.Value });

        characterCount.Should().Be(1);
        skillCount.Should().Be(2);
        maxLevelSkillCount.Should().Be(2);
        isDefault.Should().Be(1);
    }

    private async Task<IDbConnectionFactory> CreateMigratedDatabaseAsync()
    {
        Directory.CreateDirectory(_rootDirectory);

        IDbConnectionFactory connectionFactory = new SqliteConnectionFactory($"Data Source={_databasePath};Pooling=False");
        SqliteMigrationRunner migrationRunner = new(connectionFactory);
        await migrationRunner.RunAsync();
        return connectionFactory;
    }

    private static async Task SeedSkillTypesAsync(IDbConnectionFactory connectionFactory)
    {
        using System.Data.IDbConnection connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync("INSERT INTO ITEM_LOOKUP (typeID, typeName, groupName, categoryName) VALUES (@typeID, @typeName, @groupName, @categoryName)", new[]
        {
            new { typeID = 3380, typeName = "Industry", groupName = "Skill", categoryName = "Skill" },
            new { typeID = 11442, typeName = "Advanced Industry", groupName = "Skill", categoryName = "Skill" },
            new { typeID = 587, typeName = "Rifter", groupName = "Frigate", categoryName = "Ship" },
        });
    }
}