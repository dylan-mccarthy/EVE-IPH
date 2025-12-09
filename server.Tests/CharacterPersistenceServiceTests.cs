using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using server.Infrastructure;
using server.Models;
using server.Services.Characters;
using Xunit;

namespace server.Tests.Services;

public class CharacterPersistenceServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ISqliteConnectionFactory _connectionFactory;
    private readonly Mock<ILogger<CharacterPersistenceService>> _mockLogger;
    private readonly CharacterPersistenceService _service;

    public CharacterPersistenceServiceTests()
    {
        // Use in-memory SQLite database for testing
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Create necessary tables
        CreateTestTables();

        _connectionFactory = new TestSqliteConnectionFactory(_connection);
        _mockLogger = new Mock<ILogger<CharacterPersistenceService>>();
        _service = new CharacterPersistenceService(_connectionFactory, _mockLogger.Object);
    }

    [Fact]
    public async Task SaveSkillsAsync_WithNewSkills_InsertsSkills()
    {
        // Arrange
        var characterId = 12345L;
        var skills = new CharacterSkillsResponse(
            1000000,
            50000,
            new List<SkillGroup>
            {
                new SkillGroup(1, "Gunnery", new List<CharacterSkill>
                {
                    new CharacterSkill(3300, "Gunnery", 5, 256000, 5),
                    new CharacterSkill(3301, "Small Hybrid Turret", 4, 181020, 4)
                }),
                new SkillGroup(2, "Spaceship Command", new List<CharacterSkill>
                {
                    new CharacterSkill(3327, "Spaceship Command", 5, 256000, 5)
                })
            });

        // Act
        await _service.SaveSkillsAsync(characterId, skills);

        // Assert
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM CHARACTER_SKILLS WHERE CHARACTER_ID = @characterId";
        cmd.Parameters.AddWithValue("@characterId", characterId);
        var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        count.Should().Be(3);
    }

    [Fact]
    public async Task SaveSkillsAsync_WithExistingSkills_ReplacesSkills()
    {
        // Arrange
        var characterId = 12345L;

        // Insert initial skills
        var initialSkills = new CharacterSkillsResponse(
            500000,
            0,
            new List<SkillGroup>
            {
                new SkillGroup(1, "Gunnery", new List<CharacterSkill>
                {
                    new CharacterSkill(3300, "Gunnery", 3, 64000, 3)
                })
            });
        await _service.SaveSkillsAsync(characterId, initialSkills);

        // New skills with updated level
        var updatedSkills = new CharacterSkillsResponse(
            1000000,
            50000,
            new List<SkillGroup>
            {
                new SkillGroup(1, "Gunnery", new List<CharacterSkill>
                {
                    new CharacterSkill(3300, "Gunnery", 5, 256000, 5),
                    new CharacterSkill(3301, "Small Hybrid Turret", 4, 181020, 4)
                })
            });

        // Act
        await _service.SaveSkillsAsync(characterId, updatedSkills);

        // Assert
        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT SKILL_LEVEL, SKILL_POINTS 
            FROM CHARACTER_SKILLS 
            WHERE CHARACTER_ID = @characterId AND SKILL_TYPE_ID = 3300";
        cmd.Parameters.AddWithValue("@characterId", characterId);
        
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        
        var level = reader.GetInt32(0);
        var sp = reader.GetInt64(1);

        level.Should().Be(5);
        sp.Should().Be(256000);

        // Verify total count is 2 (old skill replaced, new skill added)
        var countCmd = _connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM CHARACTER_SKILLS WHERE CHARACTER_ID = @characterId";
        countCmd.Parameters.AddWithValue("@characterId", characterId);
        var count = Convert.ToInt64(await countCmd.ExecuteScalarAsync());
        count.Should().Be(2);
    }

    [Fact]
    public async Task SaveSkillsAsync_WithEmptySkills_DeletesAllSkills()
    {
        // Arrange
        var characterId = 12345L;

        // Insert initial skills
        var initialSkills = new CharacterSkillsResponse(
            500000,
            0,
            new List<SkillGroup>
            {
                new SkillGroup(1, "Gunnery", new List<CharacterSkill>
                {
                    new CharacterSkill(3300, "Gunnery", 3, 64000, 3)
                })
            });
        await _service.SaveSkillsAsync(characterId, initialSkills);

        // Empty skills
        var emptySkills = new CharacterSkillsResponse(0, 0, new List<SkillGroup>());

        // Act
        await _service.SaveSkillsAsync(characterId, emptySkills);

        // Assert
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM CHARACTER_SKILLS WHERE CHARACTER_ID = @characterId";
        cmd.Parameters.AddWithValue("@characterId", characterId);
        var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        count.Should().Be(0);
    }

    [Fact]
    public async Task SaveSkillsAsync_WithMultipleCharacters_IsolatesData()
    {
        // Arrange
        var characterId1 = 12345L;
        var characterId2 = 67890L;

        var skills1 = new CharacterSkillsResponse(
            1000000,
            0,
            new List<SkillGroup>
            {
                new SkillGroup(1, "Gunnery", new List<CharacterSkill>
                {
                    new CharacterSkill(3300, "Gunnery", 5, 256000, 5)
                })
            });

        var skills2 = new CharacterSkillsResponse(
            500000,
            0,
            new List<SkillGroup>
            {
                new SkillGroup(2, "Navigation", new List<CharacterSkill>
                {
                    new CharacterSkill(3449, "Navigation", 4, 181020, 4)
                })
            });

        // Act
        await _service.SaveSkillsAsync(characterId1, skills1);
        await _service.SaveSkillsAsync(characterId2, skills2);

        // Assert
        var cmd1 = _connection.CreateCommand();
        cmd1.CommandText = "SELECT COUNT(*) FROM CHARACTER_SKILLS WHERE CHARACTER_ID = @characterId";
        cmd1.Parameters.AddWithValue("@characterId", characterId1);
        var count1 = Convert.ToInt64(await cmd1.ExecuteScalarAsync());

        var cmd2 = _connection.CreateCommand();
        cmd2.CommandText = "SELECT COUNT(*) FROM CHARACTER_SKILLS WHERE CHARACTER_ID = @characterId";
        cmd2.Parameters.AddWithValue("@characterId", characterId2);
        var count2 = Convert.ToInt64(await cmd2.ExecuteScalarAsync());

        count1.Should().Be(1);
        count2.Should().Be(1);
    }

    private void CreateTestTables()
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE CHARACTER_SKILLS (
                CHARACTER_ID INTEGER NOT NULL,
                SKILL_TYPE_ID INTEGER NOT NULL,
                SKILL_LEVEL INTEGER NOT NULL,
                SKILL_POINTS INTEGER NOT NULL,
                PRIMARY KEY (CHARACTER_ID, SKILL_TYPE_ID)
            )";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    private sealed class TestSqliteConnectionFactory : ISqliteConnectionFactory
    {
        private readonly SqliteConnection _connection;

        public TestSqliteConnectionFactory(SqliteConnection connection)
        {
            _connection = connection;
        }

        public SqliteConnection Create() => _connection;
    }
}
