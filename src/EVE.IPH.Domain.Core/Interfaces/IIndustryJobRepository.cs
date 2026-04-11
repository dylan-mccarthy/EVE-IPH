using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Persists and retrieves stored industry jobs grouped by installer.
/// </summary>
public interface IIndustryJobRepository
{
    Task<Result<IReadOnlyList<IndustryJobRecord>>> GetByInstallerIdAsync(
        CharacterId installerId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<IndustryJobRecord>>> ReplaceAsync(
        CharacterId installerId,
        IReadOnlyList<IndustryJobRecord> jobs,
        CancellationToken cancellationToken = default);
}

/// <summary>A stored industry-job row.</summary>
public sealed record IndustryJobRecord(
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