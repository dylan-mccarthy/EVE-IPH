using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Interfaces;

namespace EVE.IPH.Infrastructure.ESI;

/// <summary>
/// Typed HTTP client over the EVE Swagger Interface.
/// </summary>
public sealed class EsiClient(HttpClient httpClient) : IEsiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public Task<Result<EsiCharacterProfile>> GetCharacterProfileAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default) =>
        GetAsync<CharacterProfileDto, EsiCharacterProfile>(
            $"characters/{characterId.Value}/?datasource=tranquility",
            dto => Result<EsiCharacterProfile>.Success(
                new EsiCharacterProfile(
                    characterId,
                    dto.Name,
                    new CorporationId(dto.CorporationId),
                    dto.AllianceId.HasValue ? Maybe<AllianceId>.Some(new AllianceId(dto.AllianceId.Value)) : Maybe<AllianceId>.None)),
            cancellationToken);

    public Task<Result<IReadOnlyList<EsiSkill>>> GetSkillsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default) =>
        GetAsync<SkillsResponseDto, IReadOnlyList<EsiSkill>>(
            $"characters/{characterId.Value}/skills/?datasource=tranquility",
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
        GetAsync<IReadOnlyList<StandingDto>, IReadOnlyList<EsiStanding>>(
            $"characters/{characterId.Value}/standings/?datasource=tranquility",
            dto => Result<IReadOnlyList<EsiStanding>>.Success(
                dto.Select(standing => new EsiStanding(standing.FromId, standing.FromType, standing.Standing)).ToList()),
            cancellationToken);

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

    private sealed record CharacterProfileDto(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("corporation_id")] long CorporationId,
        [property: JsonPropertyName("alliance_id")] long? AllianceId);

    private sealed record SkillsResponseDto(
        [property: JsonPropertyName("skills")] IReadOnlyList<SkillDto> Skills);

    private sealed record SkillDto(
        [property: JsonPropertyName("skill_id")] long SkillId,
        [property: JsonPropertyName("active_skill_level")] int ActiveSkillLevel,
        [property: JsonPropertyName("trained_skill_level")] int TrainedSkillLevel,
        [property: JsonPropertyName("skillpoints_in_skill")] long SkillPointsInSkill);

    private sealed record StandingDto(
        [property: JsonPropertyName("from_id")] long FromId,
        [property: JsonPropertyName("from_type")] string FromType,
        [property: JsonPropertyName("standing")] double Standing);
}