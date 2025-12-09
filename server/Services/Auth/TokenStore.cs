using Microsoft.Data.Sqlite;
using server.Infrastructure;
using server.Models;

namespace server.Services.Auth;

public sealed class TokenStore : ITokenStore
{
    private readonly ISqliteConnectionFactory _dbFactory;
    private readonly ILogger<TokenStore> _logger;

    public TokenStore(ISqliteConnectionFactory dbFactory, ILogger<TokenStore> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task SaveTokenAsync(long characterId, string accessToken, DateTimeOffset expiresAt, string refreshToken, string scopes, CancellationToken ct = default)
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
            // Update existing record
            var sql = @"
                UPDATE ESI_CHARACTER_DATA 
                SET ACCESS_TOKEN = @accessToken,
                    ACCESS_TOKEN_EXPIRE_DATE_TIME = @expiresAt,
                    REFRESH_TOKEN = @refreshToken,
                    SCOPES = @scopes,
                    TOKEN_TYPE = 'Bearer'
                WHERE CHARACTER_ID = @characterId";

            await using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@characterId", characterId);
            cmd.Parameters.AddWithValue("@accessToken", accessToken);
            cmd.Parameters.AddWithValue("@expiresAt", expiresAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@refreshToken", refreshToken);
            cmd.Parameters.AddWithValue("@scopes", scopes);

            await cmd.ExecuteNonQueryAsync(ct);
            _logger.LogInformation("Updated token for character {CharacterId}", characterId);
        }
        else
        {
            _logger.LogWarning("Cannot save token for non-existent character {CharacterId}", characterId);
        }
    }

    public async Task<StoredToken?> GetTokenAsync(long characterId, CancellationToken ct = default)
    {
        await using var conn = _dbFactory.Create();
        await conn.OpenAsync(ct);

        var sql = @"
            SELECT CHARACTER_ID, ACCESS_TOKEN, ACCESS_TOKEN_EXPIRE_DATE_TIME, REFRESH_TOKEN, SCOPES
            FROM ESI_CHARACTER_DATA
            WHERE CHARACTER_ID = @characterId";

        await using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@characterId", characterId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        
        if (await reader.ReadAsync(ct))
        {
            var charId = reader.GetInt64(0);
            var accessToken = reader.GetString(1);
            var expiresAtStr = reader.GetString(2);
            var refreshToken = reader.GetString(3);
            var scopes = reader.GetString(4);

            var expiresAt = DateTimeOffset.Parse(expiresAtStr);

            return new StoredToken(charId, accessToken, expiresAt, refreshToken, scopes);
        }

        return null;
    }

    public async Task UpdateTokenAsync(long characterId, string accessToken, DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        await using var conn = _dbFactory.Create();
        await conn.OpenAsync(ct);

        var sql = @"
            UPDATE ESI_CHARACTER_DATA 
            SET ACCESS_TOKEN = @accessToken,
                ACCESS_TOKEN_EXPIRE_DATE_TIME = @expiresAt
            WHERE CHARACTER_ID = @characterId";

        await using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@characterId", characterId);
        cmd.Parameters.AddWithValue("@accessToken", accessToken);
        cmd.Parameters.AddWithValue("@expiresAt", expiresAt.ToString("yyyy-MM-dd HH:mm:ss"));

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        
        if (rows > 0)
        {
            _logger.LogInformation("Refreshed token for character {CharacterId}, expires at {ExpiresAt}", characterId, expiresAt);
        }
        else
        {
            _logger.LogWarning("Failed to update token for character {CharacterId} - character not found", characterId);
        }
    }
}
