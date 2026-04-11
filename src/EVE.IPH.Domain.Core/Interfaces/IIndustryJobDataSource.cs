using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Fetches industry jobs from an external source for either a character or a corporation.
/// </summary>
public interface IIndustryJobDataSource
{
    Task<Result<IReadOnlyList<IndustryJobData>>> GetCharacterJobsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<IndustryJobData>>> GetCorporationJobsAsync(
        CorporationId corporationId,
        CancellationToken cancellationToken = default);
}

/// <summary>Current industry-job data returned by an external source.</summary>
public sealed record IndustryJobData(
    long JobId,
    CharacterId InstallerId,
    long FacilityId,
    long LocationId,
    int ActivityId,
    long BlueprintId,
    TypeId BlueprintTypeId,
    long BlueprintLocationId,
    long OutputLocationId,
    long Runs,
    double Cost,
    int LicensedRuns,
    double Probability,
    Maybe<TypeId> ProductTypeId,
    string Status,
    int Duration,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    DateTimeOffset? PauseDate,
    DateTimeOffset? CompletedDate,
    Maybe<CharacterId> CompletedCharacterId,
    int SuccessfulRuns,
    IndustryJobScope Scope);