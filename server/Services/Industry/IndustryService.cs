using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using server.Services.Auth;

namespace server.Services.Industry;

public sealed class IndustryService : IIndustryService
{
    private readonly HttpClient _client;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<IndustryService> _logger;

    public IndustryService(HttpClient client, ITokenStore tokenStore, ILogger<IndustryService> logger)
    {
        _client = client;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    public async Task<List<IndustryJob>> GetIndustryJobsAsync(long characterId, bool includeCompleted = true, CancellationToken ct = default)
    {
        var token = await _tokenStore.GetTokenAsync(characterId, ct);
        if (token == null)
        {
            _logger.LogWarning("No token found for character {CharacterId}", characterId);
            throw new InvalidOperationException($"No authentication token found for character {characterId}");
        }

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        var url = $"characters/{characterId}/industry/jobs/?datasource=tranquility";
        if (includeCompleted)
        {
            url += "&include_completed=true";
        }

        var response = await _client.GetAsync(url, ct);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch industry jobs for character {CharacterId}: {StatusCode}", 
                characterId, response.StatusCode);
            return new List<IndustryJob>();
        }

        var esiData = await response.Content.ReadFromJsonAsync<List<EsiIndustryJob>>(ct);
        return esiData?.Select(j => new IndustryJob(
            j.JobId,
            j.InstallerId,
            j.FacilityId,
            j.LocationId,
            j.ActivityId,
            j.BlueprintId,
            j.BlueprintTypeId,
            j.BlueprintLocationId,
            j.OutputLocationId,
            j.Runs,
            j.Cost ?? 0,
            j.LicensedRuns ?? 0,
            j.Probability ?? 0,
            j.ProductTypeId,
            j.Status,
            j.Duration,
            j.StartDate,
            j.EndDate,
            j.PauseDate,
            j.CompletedDate,
            j.CompletedCharacterId,
            j.SuccessfulRuns
        )).ToList() ?? new List<IndustryJob>();
    }

    private sealed record EsiIndustryJob(
        [property: JsonPropertyName("job_id")] int JobId,
        [property: JsonPropertyName("installer_id")] int InstallerId,
        [property: JsonPropertyName("facility_id")] long FacilityId,
        [property: JsonPropertyName("location_id")] long LocationId,
        [property: JsonPropertyName("activity_id")] int ActivityId,
        [property: JsonPropertyName("blueprint_id")] int BlueprintId,
        [property: JsonPropertyName("blueprint_type_id")] long BlueprintTypeId,
        [property: JsonPropertyName("blueprint_location_id")] long BlueprintLocationId,
        [property: JsonPropertyName("output_location_id")] long OutputLocationId,
        [property: JsonPropertyName("runs")] int Runs,
        [property: JsonPropertyName("cost")] decimal? Cost,
        [property: JsonPropertyName("licensed_runs")] int? LicensedRuns,
        [property: JsonPropertyName("probability")] double? Probability,
        [property: JsonPropertyName("product_type_id")] int? ProductTypeId,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("duration")] int Duration,
        [property: JsonPropertyName("start_date")] DateTimeOffset StartDate,
        [property: JsonPropertyName("end_date")] DateTimeOffset EndDate,
        [property: JsonPropertyName("pause_date")] DateTimeOffset? PauseDate,
        [property: JsonPropertyName("completed_date")] DateTimeOffset? CompletedDate,
        [property: JsonPropertyName("completed_character_id")] int? CompletedCharacterId,
        [property: JsonPropertyName("successful_runs")] int? SuccessfulRuns
    );
}
