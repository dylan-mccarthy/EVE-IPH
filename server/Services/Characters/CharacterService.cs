using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using server.Infrastructure;
using server.Models;
using server.Services.Auth;

namespace server.Services.Characters;

public sealed class CharacterService : ICharacterService
{
    private readonly HttpClient _http;
    private readonly EsiOptions _options;
    private readonly ISqliteConnectionFactory _dbFactory;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<CharacterService> _logger;

    public CharacterService(HttpClient http, IOptions<EsiOptions> options, ISqliteConnectionFactory dbFactory, ITokenStore tokenStore, ILogger<CharacterService> logger)
    {
        _http = http;
        _options = options.Value;
        _dbFactory = dbFactory;
        _tokenStore = tokenStore;
        _logger = logger;
        _http.BaseAddress ??= new Uri(_options.BaseUrl);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<CharacterListResponse> GetCharactersAsync(CancellationToken ct = default)
    {
        await using var conn = _dbFactory.Create();
        await conn.OpenAsync(ct);

        var sql = @"
            SELECT DISTINCT
                ecd.CHARACTER_ID,
                ecd.CHARACTER_NAME,
                ecd.CORPORATION_ID,
                ecpd.CORPORATION_NAME,
                ecd.IS_DEFAULT,
                ecd.ACCESS_TOKEN_EXPIRE_DATE_TIME
            FROM ESI_CHARACTER_DATA ecd
            LEFT JOIN ESI_CORPORATION_DATA ecpd ON ecd.CORPORATION_ID = ecpd.CORPORATION_ID
            WHERE ecd.CHARACTER_ID <> 1
            ORDER BY ecd.IS_DEFAULT DESC, ecd.CHARACTER_NAME";

        await using var cmd = new SqliteCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var characters = new List<CharacterListItem>();
        while (await reader.ReadAsync(ct))
        {
            var characterId = reader.GetInt64(0);
            var characterName = reader.GetString(1);
            var corporationId = reader.GetInt64(2);
            var corporationName = reader.IsDBNull(3) ? null : reader.GetString(3);
            var isDefault = reader.GetInt32(4) != 0;
            var tokenExpiry = reader.IsDBNull(5) ? DateTimeOffset.MinValue : DateTimeOffset.Parse(reader.GetString(5));
            var hasValidToken = tokenExpiry > DateTimeOffset.UtcNow;

            characters.Add(new CharacterListItem(
                characterId,
                characterName,
                corporationId,
                corporationName,
                isDefault,
                hasValidToken
            ));
        }

        return new CharacterListResponse(characters);
    }

    public async Task<CharacterDetails> GetCharacterDetailsAsync(long characterId, CancellationToken ct = default)
    {
        await using var conn = _dbFactory.Create();
        await conn.OpenAsync(ct);

        // Fetch character data from database
        var sql = @"
            SELECT 
                ecd.CHARACTER_ID,
                ecd.CHARACTER_NAME,
                ecd.GENDER,
                ecd.BIRTHDAY,
                ecd.RACE_ID,
                ecd.BLOODLINE_ID,
                ecd.ANCESTRY_ID,
                ecd.DESCRIPTION,
                ecd.SCOPES,
                ecd.CORPORATION_ID,
                ecpd.CORPORATION_NAME,
                ecpd.TICKER,
                ecpd.MEMBER_COUNT,
                ecpd.ALLIANCE_ID
            FROM ESI_CHARACTER_DATA ecd
            LEFT JOIN ESI_CORPORATION_DATA ecpd ON ecd.CORPORATION_ID = ecpd.CORPORATION_ID
            WHERE ecd.CHARACTER_ID = @characterId";

        await using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@characterId", characterId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
        {
            throw new InvalidOperationException($"Character {characterId} not found");
        }

        var charId = reader.GetInt64(0);
        var charName = reader.GetString(1);
        var gender = reader.GetString(2);
        var birthday = reader.GetString(3);
        var raceId = reader.GetInt32(4);
        var bloodlineId = reader.GetInt32(5);
        var ancestryId = reader.GetInt32(6);
        var description = reader.IsDBNull(7) ? null : reader.GetString(7);
        var scopes = reader.GetString(8).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var corpId = reader.GetInt64(9);
        var corpName = reader.IsDBNull(10) ? "Unknown" : reader.GetString(10);
        var ticker = reader.IsDBNull(11) ? null : reader.GetString(11);
        var memberCount = reader.IsDBNull(12) ? (int?)null : reader.GetInt32(12);
        var allianceId = reader.IsDBNull(13) ? (long?)null : reader.GetInt64(13);

        // Get alliance name if alliance exists
        string? allianceName = null;
        if (allianceId.HasValue)
        {
            var allianceSql = "SELECT ALLIANCE_NAME FROM ESI_ALLIANCE_DATA WHERE ALLIANCE_ID = @allianceId";
            await using var allianceCmd = new SqliteCommand(allianceSql, conn);
            allianceCmd.Parameters.AddWithValue("@allianceId", allianceId.Value);
            var result = await allianceCmd.ExecuteScalarAsync(ct);
            allianceName = result?.ToString();
        }

        // Get skills summary
        var skillsSql = @"
            SELECT 
                COUNT(*) as TotalSkills,
                SUM(SKILL_POINTS) as TotalSp
            FROM CHARACTER_SKILLS 
            WHERE CHARACTER_ID = @characterId";

        await using var skillsCmd = new SqliteCommand(skillsSql, conn);
        skillsCmd.Parameters.AddWithValue("@characterId", characterId);
        await using var skillsReader = await skillsCmd.ExecuteReaderAsync(ct);

        int totalSkills = 0;
        long totalSp = 0;
        if (await skillsReader.ReadAsync(ct))
        {
            totalSkills = skillsReader.IsDBNull(0) ? 0 : skillsReader.GetInt32(0);
            totalSp = skillsReader.IsDBNull(1) ? 0 : skillsReader.GetInt64(1);
        }

        // Fetch wallet balance from ESI if we have the scope
        WalletInfo? wallet = null;
        _logger.LogInformation("Character {CharacterId} scopes: {Scopes}", characterId, string.Join(", ", scopes));
        
        if (scopes.Contains("esi-wallet.read_character_wallet.v1"))
        {
            _logger.LogInformation("Character {CharacterId} has wallet scope, fetching balance", characterId);
            try
            {
                var token = await _tokenStore.GetTokenAsync(characterId, ct);
                if (token != null)
                {
                    var walletRequest = new HttpRequestMessage(HttpMethod.Get, $"characters/{characterId}/wallet/?datasource=tranquility");
                    walletRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                    
                    using var walletResponse = await _http.SendAsync(walletRequest, ct);
                    _logger.LogInformation("Wallet API response status: {StatusCode}", walletResponse.StatusCode);
                    
                    if (walletResponse.IsSuccessStatusCode)
                    {
                        var balance = await walletResponse.Content.ReadFromJsonAsync<decimal>(cancellationToken: ct);
                        wallet = new WalletInfo((double)balance);
                        _logger.LogInformation("Wallet balance for character {CharacterId}: {Balance}", characterId, balance);
                    }
                    else
                    {
                        var errorContent = await walletResponse.Content.ReadAsStringAsync(ct);
                        _logger.LogWarning("Failed to fetch wallet: {StatusCode} - {Content}", walletResponse.StatusCode, errorContent);
                    }
                }
                else
                {
                    _logger.LogWarning("No token found for character {CharacterId}", characterId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch wallet balance for character {CharacterId}", characterId);
                wallet = new WalletInfo(0);
            }
        }
        else
        {
            _logger.LogInformation("Character {CharacterId} does not have wallet scope", characterId);
        }

        var corporation = new CorporationInfo(
            corpId,
            corpName,
            ticker,
            memberCount,
            allianceId,
            allianceName
        );

        var skillsSummary = new SkillsSummary(
            totalSp,
            totalSkills,
            0 // Unallocated SP would need ESI call
        );

        return new CharacterDetails(
            charId,
            charName,
            gender,
            birthday,
            raceId,
            bloodlineId,
            ancestryId,
            description,
            null, // Security status would need ESI call
            corporation,
            wallet,
            skillsSummary,
            scopes
        );
    }

    public async Task<CharacterProfile> GetProfileAsync(long characterId, string accessToken, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"characters/{characterId}/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CharacterResponse>(cancellationToken: ct);
        if (payload is null)
        {
            throw new InvalidOperationException("Empty response from ESI.");
        }

        return new CharacterProfile(
            characterId,
            payload.name,
            payload.corporation_id,
            payload.alliance_id,
            payload.security_status,
            payload.birthday);
    }

    public async Task<CharacterSkillsResponse> GetSkillsAsync(long characterId, string accessToken, CancellationToken ct = default)
    {
        // Fetch skills from ESI
        var request = new HttpRequestMessage(HttpMethod.Get, $"characters/{characterId}/skills/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<EsiSkillsResponse>(cancellationToken: ct);
        if (payload is null)
        {
            throw new InvalidOperationException("Empty response from ESI.");
        }

        // Load skill names and groups from database
        var skillDetails = await LoadSkillDetailsFromDatabase(payload.skills.Select(s => s.skill_id).ToList(), ct);
        
        // Group skills by group name
        var grouped = skillDetails
            .GroupBy(s => s.GroupName)
            .Select(g => new SkillGroup(
                0, // Group ID not available in this schema
                g.Key,
                g.Select(s => new CharacterSkill(
                    s.SkillId,
                    s.SkillName,
                    payload.skills.First(es => es.skill_id == s.SkillId).trained_skill_level,
                    payload.skills.First(es => es.skill_id == s.SkillId).skillpoints_in_skill,
                    payload.skills.First(es => es.skill_id == s.SkillId).active_skill_level
                )).OrderBy(s => s.SkillName).ToList()
            ))
            .OrderBy(g => g.GroupName)
            .ToList();

        return new CharacterSkillsResponse(
            payload.total_sp,
            payload.unallocated_sp ?? 0,
            grouped
        );
    }

    private async Task<List<SkillDetail>> LoadSkillDetailsFromDatabase(List<long> skillIds, CancellationToken ct)
    {
        await using var conn = _dbFactory.Create();
        await conn.OpenAsync(ct);

        var idList = string.Join(",", skillIds);
        var sql = $@"
            SELECT 
                SKILL_TYPE_ID as SkillId,
                SKILL_NAME as SkillName,
                SKILL_GROUP as GroupName
            FROM SKILLS
            WHERE SKILL_TYPE_ID IN ({idList})";

        await using var cmd = new SqliteCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var results = new List<SkillDetail>();
        while (await reader.ReadAsync(ct))
        {
            results.Add(new SkillDetail(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2)
            ));
        }

        return results;
    }

    private sealed record SkillDetail(long SkillId, string SkillName, string GroupName);
    private sealed record EsiSkillsResponse(long total_sp, int? unallocated_sp, List<EsiSkill> skills);
    private sealed record EsiSkill(long skill_id, int trained_skill_level, long skillpoints_in_skill, int active_skill_level);
    private sealed record CharacterResponse(string name, int corporation_id, int? alliance_id, double? security_status, DateTime? birthday);
}
