using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Models;
using EVE.IPH.Infrastructure.ESI.Interfaces;

namespace EVE.IPH.Infrastructure.ESI;

/// <summary>
/// Typed HTTP client over the EVE Swagger Interface.
/// </summary>
public sealed class EsiClient(HttpClient httpClient, IEsiTokenProvider tokenProvider) : IEsiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly IEsiTokenProvider _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));

    public Task<Result<EsiCharacterProfile>> GetCharacterProfileAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default) =>
        GetAuthorizedAsync<CharacterProfileDto, EsiCharacterProfile>(
            $"characters/{characterId.Value}/?datasource=tranquility",
            characterId,
            dto => Result<EsiCharacterProfile>.Success(
                new EsiCharacterProfile(
                    characterId,
                    dto.Name,
                    new CorporationId(dto.CorporationId),
                    dto.AllianceId.HasValue ? Maybe<AllianceId>.Some(new AllianceId(dto.AllianceId.Value)) : Maybe<AllianceId>.None)),
            cancellationToken);

    public Task<Result<IReadOnlyList<EsiAsset>>> GetCharacterAssetsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default) =>
        GetAuthorizedAsync<IReadOnlyList<AssetDto>, IReadOnlyList<EsiAsset>>(
            $"characters/{characterId.Value}/assets/?datasource=tranquility",
            characterId,
            dto => Result<IReadOnlyList<EsiAsset>>.Success(
                dto.Select(asset => new EsiAsset(
                    characterId.Value,
                    asset.ItemId,
                    asset.LocationId,
                    new TypeId(asset.TypeId),
                    asset.Quantity,
                    asset.LocationFlag,
                    asset.IsSingleton,
                    asset.IsBlueprintCopy,
                    asset.Name ?? string.Empty)).ToList()),
            cancellationToken);

    public Task<Result<IReadOnlyList<EsiAsset>>> GetCorporationAssetsAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        CancellationToken cancellationToken = default) =>
        GetAuthorizedAsync<IReadOnlyList<AssetDto>, IReadOnlyList<EsiAsset>>(
            $"corporations/{corporationId.Value}/assets/?datasource=tranquility",
            authenticatedCharacterId,
            dto => Result<IReadOnlyList<EsiAsset>>.Success(
                dto.Select(asset => new EsiAsset(
                    corporationId.Value,
                    asset.ItemId,
                    asset.LocationId,
                    new TypeId(asset.TypeId),
                    asset.Quantity,
                    asset.LocationFlag,
                    asset.IsSingleton,
                    asset.IsBlueprintCopy,
                    asset.Name ?? string.Empty)).ToList()),
            cancellationToken);

    public Task<Result<IReadOnlyList<EsiOwnedBlueprint>>> GetCorporationBlueprintsAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        CancellationToken cancellationToken = default) =>
        GetAuthorizedAsync<IReadOnlyList<OwnedBlueprintDto>, IReadOnlyList<EsiOwnedBlueprint>>(
            $"corporations/{corporationId.Value}/blueprints/?datasource=tranquility",
            authenticatedCharacterId,
            dto => Result<IReadOnlyList<EsiOwnedBlueprint>>.Success(
                dto.Select(blueprint => new EsiOwnedBlueprint(
                    corporationId.Value,
                    new ItemId(blueprint.ItemId),
                    blueprint.LocationId,
                    new BlueprintId(blueprint.TypeId),
                    blueprint.Quantity,
                    blueprint.MaterialEfficiency,
                    blueprint.TimeEfficiency,
                    blueprint.Runs)).ToList()),
            cancellationToken);

    public Task<Result<IReadOnlyList<string>>> GetCorporationRolesAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        CancellationToken cancellationToken = default) =>
        GetAuthorizedAsync<IReadOnlyList<CorporationRoleAssignmentDto>, IReadOnlyList<string>>(
            $"corporations/{corporationId.Value}/roles/?datasource=tranquility",
            authenticatedCharacterId,
            dto =>
            {
                CorporationRoleAssignmentDto? assignment = dto.FirstOrDefault(entry => entry.CharacterId == authenticatedCharacterId.Value);
                if (assignment is null)
                {
                    return Result<IReadOnlyList<string>>.Success([]);
                }

                string[] roles = EnumerateRoles(assignment)
                    .Where(role => !string.IsNullOrWhiteSpace(role))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return Result<IReadOnlyList<string>>.Success(roles);
            },
            cancellationToken);

    public Task<Result<IReadOnlyList<EsiSkill>>> GetSkillsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default) =>
        GetAuthorizedAsync<SkillsResponseDto, IReadOnlyList<EsiSkill>>(
            $"characters/{characterId.Value}/skills/?datasource=tranquility",
            characterId,
            dto => Result<IReadOnlyList<EsiSkill>>.Success(
                dto.Skills
                    .Select(skill => new EsiSkill(
                        new TypeId(skill.SkillId),
                        skill.ActiveSkillLevel,
                        skill.TrainedSkillLevel,
                        skill.SkillPointsInSkill))
                    .ToList()),
            cancellationToken);

    public Task<Result<IReadOnlyList<EsiStanding>>> GetStandingsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default) =>
        GetAuthorizedAsync<IReadOnlyList<StandingDto>, IReadOnlyList<EsiStanding>>(
            $"characters/{characterId.Value}/standings/?datasource=tranquility",
            characterId,
            dto => Result<IReadOnlyList<EsiStanding>>.Success(
                dto.Select(standing => new EsiStanding(standing.FromId, standing.FromType, standing.Standing)).ToList()),
            cancellationToken);

    public Task<Result<IReadOnlyList<EsiResearchAgent>>> GetResearchAgentsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default) =>
        GetAuthorizedAsync<IReadOnlyList<ResearchAgentDto>, IReadOnlyList<EsiResearchAgent>>(
            $"characters/{characterId.Value}/agents_research/?datasource=tranquility",
            characterId,
            dto => Result<IReadOnlyList<EsiResearchAgent>>.Success(
                dto.Select(agent => new EsiResearchAgent(
                    agent.AgentId,
                    new TypeId(agent.SkillTypeId),
                    agent.StartedAt,
                    agent.PointsPerDay,
                    agent.RemainderPoints)).ToList()),
            cancellationToken);

    public Task<Result<IReadOnlyList<EsiIndustryJob>>> GetCharacterIndustryJobsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default) =>
        GetIndustryJobsAsync(
            $"characters/{characterId.Value}/industry/jobs/?datasource=tranquility&include_completed=true",
            characterId,
            IndustryJobScope.Personal,
            cancellationToken);

    public Task<Result<IReadOnlyList<EsiIndustryJob>>> GetCorporationIndustryJobsAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        CancellationToken cancellationToken = default) =>
        GetIndustryJobsAsync(
            $"corporations/{corporationId.Value}/industry/jobs/?datasource=tranquility&include_completed=true",
            authenticatedCharacterId,
            IndustryJobScope.Corporation,
            cancellationToken);

    public async Task<Result<IReadOnlyList<EsiEntityName>>> GetNamesAsync(
        IReadOnlyList<long> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return Result<IReadOnlyList<EsiEntityName>>.Success([]);
        }

        try
        {
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
                "universe/names/?datasource=tranquility",
                ids,
                SerializerOptions,
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return Result<IReadOnlyList<EsiEntityName>>.Failure(
                    $"ESI_{(int)response.StatusCode}",
                    string.IsNullOrWhiteSpace(message)
                        ? $"ESI request failed with status code {(int)response.StatusCode}."
                        : message);
            }

            IReadOnlyList<EntityNameDto>? dto = await response.Content.ReadFromJsonAsync<IReadOnlyList<EntityNameDto>>(
                SerializerOptions,
                cancellationToken).ConfigureAwait(false);

            if (dto is null)
            {
                return Result<IReadOnlyList<EsiEntityName>>.Failure("ESI_EMPTY_RESPONSE", "ESI returned an empty response body.");
            }

            return Result<IReadOnlyList<EsiEntityName>>.Success(
                dto.Select(entry => new EsiEntityName(entry.Id, entry.Category, entry.Name)).ToList());
        }
        catch (HttpRequestException ex)
        {
            return Result<IReadOnlyList<EsiEntityName>>.Failure("ESI_HTTP_ERROR", ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyList<EsiEntityName>>.Failure("ESI_TIMEOUT", ex.Message);
        }
        catch (JsonException ex)
        {
            return Result<IReadOnlyList<EsiEntityName>>.Failure("ESI_INVALID_JSON", ex.Message);
        }
    }

    private async Task<Result<TModel>> GetAsync<TDto, TModel>(
        string relativeUri,
        Func<TDto, Result<TModel>> map,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(relativeUri, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                string message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return Result<TModel>.Failure(
                    $"ESI_{(int)response.StatusCode}",
                    string.IsNullOrWhiteSpace(message)
                        ? $"ESI request failed with status code {(int)response.StatusCode}."
                        : message);
            }

            TDto? dto = await response.Content.ReadFromJsonAsync<TDto>(SerializerOptions, cancellationToken).ConfigureAwait(false);
            if (dto is null)
            {
                return Result<TModel>.Failure("ESI_EMPTY_RESPONSE", "ESI returned an empty response body.");
            }

            return map(dto);
        }
        catch (HttpRequestException ex)
        {
            return Result<TModel>.Failure("ESI_HTTP_ERROR", ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<TModel>.Failure("ESI_TIMEOUT", ex.Message);
        }
        catch (JsonException ex)
        {
            return Result<TModel>.Failure("ESI_INVALID_JSON", ex.Message);
        }
    }

    private async Task<Result<TModel>> GetAuthorizedAsync<TDto, TModel>(
        string relativeUri,
        CharacterId authenticatedCharacterId,
        Func<TDto, Result<TModel>> map,
        CancellationToken cancellationToken)
    {
        Result<EsiAccessToken> tokenResult = await _tokenProvider.GetAccessTokenAsync(authenticatedCharacterId, cancellationToken).ConfigureAwait(false);
        if (tokenResult.IsFailure)
        {
            return Result<TModel>.Failure(tokenResult.Error);
        }

        Result<TModel> result = await GetAsync(relativeUri, tokenResult.Value.AccessToken, map, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure && result.Error.Code == "ESI_401")
        {
            Result<EsiAccessToken> refreshResult = await _tokenProvider.RefreshAccessTokenAsync(authenticatedCharacterId, cancellationToken).ConfigureAwait(false);
            if (refreshResult.IsFailure)
            {
                return Result<TModel>.Failure(refreshResult.Error);
            }

            return await GetAsync(relativeUri, refreshResult.Value.AccessToken, map, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    private async Task<Result<TModel>> GetAsync<TDto, TModel>(
        string relativeUri,
        string accessToken,
        Func<TDto, Result<TModel>> map,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, relativeUri);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        return await SendAsync<TDto, TModel>(request, map, cancellationToken).ConfigureAwait(false);
    }

    private Task<Result<IReadOnlyList<EsiIndustryJob>>> GetIndustryJobsAsync(
        string relativeUri,
        CharacterId authenticatedCharacterId,
        IndustryJobScope scope,
        CancellationToken cancellationToken) =>
        GetAuthorizedAsync<IReadOnlyList<IndustryJobDto>, IReadOnlyList<EsiIndustryJob>>(
            relativeUri,
            authenticatedCharacterId,
            dto => Result<IReadOnlyList<EsiIndustryJob>>.Success(dto.Select(job => new EsiIndustryJob(
                job.JobId,
                new CharacterId(job.InstallerId),
                job.FacilityId,
                job.LocationId ?? 0,
                job.ActivityId,
                job.BlueprintId,
                new TypeId(job.BlueprintTypeId),
                job.BlueprintLocationId,
                job.OutputLocationId,
                job.Runs,
                job.Cost,
                job.LicensedRuns,
                job.Probability,
                job.ProductTypeId.HasValue ? Maybe<TypeId>.Some(new TypeId(job.ProductTypeId.Value)) : Maybe<TypeId>.None,
                job.Status,
                job.Duration,
                job.StartDate,
                job.EndDate,
                job.PauseDate,
                job.CompletedDate,
                job.CompletedCharacterId.HasValue ? Maybe<CharacterId>.Some(new CharacterId(job.CompletedCharacterId.Value)) : Maybe<CharacterId>.None,
                job.SuccessfulRuns,
                scope)).ToList()),
            cancellationToken);

    private async Task<Result<TModel>> SendAsync<TDto, TModel>(
        HttpRequestMessage request,
        Func<TDto, Result<TModel>> map,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                string message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return Result<TModel>.Failure(
                    $"ESI_{(int)response.StatusCode}",
                    string.IsNullOrWhiteSpace(message)
                        ? $"ESI request failed with status code {(int)response.StatusCode}."
                        : message);
            }

            TDto? dto = await response.Content.ReadFromJsonAsync<TDto>(SerializerOptions, cancellationToken).ConfigureAwait(false);
            if (dto is null)
            {
                return Result<TModel>.Failure("ESI_EMPTY_RESPONSE", "ESI returned an empty response body.");
            }

            return map(dto);
        }
        catch (HttpRequestException ex)
        {
            return Result<TModel>.Failure("ESI_HTTP_ERROR", ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<TModel>.Failure("ESI_TIMEOUT", ex.Message);
        }
        catch (JsonException ex)
        {
            return Result<TModel>.Failure("ESI_INVALID_JSON", ex.Message);
        }
    }

    private sealed record CharacterProfileDto(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("corporation_id")] long CorporationId,
        [property: JsonPropertyName("alliance_id")] long? AllianceId);

    private sealed record SkillsResponseDto(
        [property: JsonPropertyName("skills")] IReadOnlyList<SkillDto> Skills);

    private sealed record AssetDto(
        [property: JsonPropertyName("item_id")] long ItemId,
        [property: JsonPropertyName("location_id")] long LocationId,
        [property: JsonPropertyName("type_id")] long TypeId,
        [property: JsonPropertyName("quantity")] long Quantity,
        [property: JsonPropertyName("location_flag")] int LocationFlag,
        [property: JsonPropertyName("is_singleton")] bool IsSingleton,
        [property: JsonPropertyName("is_blueprint_copy")] bool IsBlueprintCopy,
        [property: JsonPropertyName("name")] string? Name);

    private sealed record OwnedBlueprintDto(
        [property: JsonPropertyName("item_id")] long ItemId,
        [property: JsonPropertyName("location_id")] long LocationId,
        [property: JsonPropertyName("type_id")] long TypeId,
        [property: JsonPropertyName("quantity")] int Quantity,
        [property: JsonPropertyName("time_efficiency")] int TimeEfficiency,
        [property: JsonPropertyName("material_efficiency")] int MaterialEfficiency,
        [property: JsonPropertyName("runs")] int Runs);

    private sealed record SkillDto(
        [property: JsonPropertyName("skill_id")] long SkillId,
        [property: JsonPropertyName("active_skill_level")] int ActiveSkillLevel,
        [property: JsonPropertyName("trained_skill_level")] int TrainedSkillLevel,
        [property: JsonPropertyName("skillpoints_in_skill")] long SkillPointsInSkill);

    private sealed record StandingDto(
        [property: JsonPropertyName("from_id")] long FromId,
        [property: JsonPropertyName("from_type")] string FromType,
        [property: JsonPropertyName("standing")] double Standing);

    private sealed record ResearchAgentDto(
        [property: JsonPropertyName("agent_id")] long AgentId,
        [property: JsonPropertyName("skill_type_id")] long SkillTypeId,
        [property: JsonPropertyName("started_at")] DateTimeOffset StartedAt,
        [property: JsonPropertyName("points_per_day")] double PointsPerDay,
        [property: JsonPropertyName("remainder_points")] double RemainderPoints);

    private sealed record CorporationRoleAssignmentDto(
        [property: JsonPropertyName("character_id")] long CharacterId,
        [property: JsonPropertyName("roles")] IReadOnlyList<string>? Roles,
        [property: JsonPropertyName("roles_at_base")] IReadOnlyList<string>? RolesAtBase,
        [property: JsonPropertyName("roles_at_hq")] IReadOnlyList<string>? RolesAtHq,
        [property: JsonPropertyName("roles_at_other")] IReadOnlyList<string>? RolesAtOther);

    private sealed record EntityNameDto(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("name")] string Name);

    private sealed record IndustryJobDto(
        [property: JsonPropertyName("job_id")] long JobId,
        [property: JsonPropertyName("installer_id")] long InstallerId,
        [property: JsonPropertyName("facility_id")] long FacilityId,
        [property: JsonPropertyName("location_id")] long? LocationId,
        [property: JsonPropertyName("activity_id")] int ActivityId,
        [property: JsonPropertyName("blueprint_id")] long BlueprintId,
        [property: JsonPropertyName("blueprint_type_id")] long BlueprintTypeId,
        [property: JsonPropertyName("blueprint_location_id")] long BlueprintLocationId,
        [property: JsonPropertyName("output_location_id")] long OutputLocationId,
        [property: JsonPropertyName("runs")] long Runs,
        [property: JsonPropertyName("cost")] double Cost,
        [property: JsonPropertyName("licensed_runs")] int LicensedRuns,
        [property: JsonPropertyName("probability")] double Probability,
        [property: JsonPropertyName("product_type_id")] long? ProductTypeId,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("duration")] int Duration,
        [property: JsonPropertyName("start_date")] DateTimeOffset? StartDate,
        [property: JsonPropertyName("end_date")] DateTimeOffset? EndDate,
        [property: JsonPropertyName("pause_date")] DateTimeOffset? PauseDate,
        [property: JsonPropertyName("completed_date")] DateTimeOffset? CompletedDate,
        [property: JsonPropertyName("completed_character_id")] long? CompletedCharacterId,
        [property: JsonPropertyName("successful_runs")] int SuccessfulRuns);

    private static IEnumerable<string> EnumerateRoles(CorporationRoleAssignmentDto assignment)
    {
        return Enumerate(assignment.Roles)
            .Concat(Enumerate(assignment.RolesAtBase))
            .Concat(Enumerate(assignment.RolesAtHq))
            .Concat(Enumerate(assignment.RolesAtOther));

        static IEnumerable<string> Enumerate(IReadOnlyList<string>? roles) => roles ?? [];
    }
}