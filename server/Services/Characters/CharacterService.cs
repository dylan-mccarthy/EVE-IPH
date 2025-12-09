using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using server.Infrastructure;
using server.Models;

namespace server.Services.Characters;

public sealed class CharacterService : ICharacterService
{
    private readonly HttpClient _http;
    private readonly EsiOptions _options;
    private readonly ISqliteConnectionFactory _dbFactory;

    public CharacterService(HttpClient http, IOptions<EsiOptions> options, ISqliteConnectionFactory dbFactory)
    {
        _http = http;
        _options = options.Value;
        _dbFactory = dbFactory;
        _http.BaseAddress ??= new Uri(_options.BaseUrl);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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
