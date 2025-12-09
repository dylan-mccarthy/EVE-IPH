using Microsoft.Data.Sqlite;
using server.Infrastructure;
using server.Models;

namespace server.Services.Characters;

public interface ICharacterPersistenceService
{
    Task SaveCharacterAsync(long characterId, string characterName, CharacterProfile profile, string scopes, CancellationToken ct = default);
}

public sealed class CharacterPersistenceService : ICharacterPersistenceService
{
    private readonly ISqliteConnectionFactory _dbFactory;
    private readonly ILogger<CharacterPersistenceService> _logger;

    public CharacterPersistenceService(ISqliteConnectionFactory dbFactory, ILogger<CharacterPersistenceService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task SaveCharacterAsync(long characterId, string characterName, CharacterProfile profile, string scopes, CancellationToken ct = default)
    {
        await using var conn = _dbFactory.Create();
        await conn.OpenAsync(ct);

        // Check if character exists
        var checkSql = "SELECT COUNT(*) FROM ESI_CHARACTER_DATA WHERE CHARACTER_ID = @characterId";
        await using var checkCmd = new SqliteCommand(checkSql, conn);
        checkCmd.Parameters.AddWithValue("@characterId", characterId);
        var exists = Convert.ToInt64(await checkCmd.ExecuteScalarAsync(ct)) > 0;

        if (exists)
        {
            // Update existing character
            var updateSql = @"
                UPDATE ESI_CHARACTER_DATA 
                SET CHARACTER_NAME = @name,
                    CORPORATION_ID = @corpId,
                    BIRTHDAY = @birthday,
                    SCOPES = @scopes
                WHERE CHARACTER_ID = @characterId";

            await using var updateCmd = new SqliteCommand(updateSql, conn);
            updateCmd.Parameters.AddWithValue("@characterId", characterId);
            updateCmd.Parameters.AddWithValue("@name", characterName);
            updateCmd.Parameters.AddWithValue("@corpId", profile.CorporationId);
            updateCmd.Parameters.AddWithValue("@birthday", profile.Birthday?.ToString("yyyy-MM-dd HH:mm:ss") ?? "");
            updateCmd.Parameters.AddWithValue("@scopes", scopes);

            await updateCmd.ExecuteNonQueryAsync(ct);
            _logger.LogInformation("Updated character {CharacterId} ({CharacterName})", characterId, characterName);
        }
        else
        {
            // Insert new character - we provide default values for fields we don't have yet
            // These will be filled in later when we fetch more data
            var insertSql = @"
                INSERT INTO ESI_CHARACTER_DATA (
                    CHARACTER_ID, CHARACTER_NAME, CORPORATION_ID, BIRTHDAY, GENDER, 
                    RACE_ID, BLOODLINE_ID, ANCESTRY_ID, DESCRIPTION, SCOPES,
                    ACCESS_TOKEN, ACCESS_TOKEN_EXPIRE_DATE_TIME, TOKEN_TYPE, REFRESH_TOKEN,
                    OVERRIDE_SKILLS, IS_DEFAULT
                ) VALUES (
                    @characterId, @name, @corpId, @birthday, '',
                    0, 0, 0, '', @scopes,
                    '', '2000-01-01 00:00:00', 'Bearer', '',
                    0, 0
                )";

            await using var insertCmd = new SqliteCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@characterId", characterId);
            insertCmd.Parameters.AddWithValue("@name", characterName);
            insertCmd.Parameters.AddWithValue("@corpId", profile.CorporationId);
            insertCmd.Parameters.AddWithValue("@birthday", profile.Birthday?.ToString("yyyy-MM-dd HH:mm:ss") ?? "");
            insertCmd.Parameters.AddWithValue("@scopes", scopes);

            await insertCmd.ExecuteNonQueryAsync(ct);
            _logger.LogInformation("Inserted new character {CharacterId} ({CharacterName})", characterId, characterName);
        }

        // Ensure corporation data exists
        await EnsureCorporationDataAsync(profile.CorporationId, conn, ct);
    }

    private async Task EnsureCorporationDataAsync(long corporationId, SqliteConnection conn, CancellationToken ct)
    {
        var checkSql = "SELECT COUNT(*) FROM ESI_CORPORATION_DATA WHERE CORPORATION_ID = @corpId";
        await using var checkCmd = new SqliteCommand(checkSql, conn);
        checkCmd.Parameters.AddWithValue("@corpId", corporationId);
        var exists = Convert.ToInt64(await checkCmd.ExecuteScalarAsync(ct)) > 0;

        if (!exists)
        {
            // Insert placeholder corporation data
            var insertSql = @"
                INSERT INTO ESI_CORPORATION_DATA (
                    CORPORATION_ID, CORPORATION_NAME, TICKER, MEMBER_COUNT,
                    FACTION_ID, ALLIANCE_ID, CEO_ID, CREATOR_ID, HOME_STATION_ID,
                    SHARES, TAX_RATE, DESCRIPTION, DATE_FOUNDED, URL
                ) VALUES (
                    @corpId, 'Unknown Corporation', 'UNK', 0,
                    NULL, NULL, 0, 0, NULL,
                    NULL, 0.0, '', NULL, ''
                )";

            await using var insertCmd = new SqliteCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@corpId", corporationId);
            await insertCmd.ExecuteNonQueryAsync(ct);
            
            _logger.LogInformation("Created placeholder corporation record for {CorporationId}", corporationId);
        }
    }
}
