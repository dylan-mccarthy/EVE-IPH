using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.ESI.Interfaces;
using EVE.IPH.Infrastructure.ESI.Models;

namespace EVE.IPH.Infrastructure.ESI;

public sealed class EsiIndustryJobDataSource(IEsiClient esiClient) : IIndustryJobDataSource
{
    private readonly IEsiClient _esiClient = esiClient ?? throw new ArgumentNullException(nameof(esiClient));

    public async Task<Result<IReadOnlyList<IndustryJobData>>> GetCharacterJobsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<EsiIndustryJob>> jobs = await _esiClient
            .GetCharacterIndustryJobsAsync(characterId, cancellationToken)
            .ConfigureAwait(false);

        return jobs.IsSuccess
            ? Result<IReadOnlyList<IndustryJobData>>.Success(jobs.Value.Select(MapJob).ToList())
            : Result<IReadOnlyList<IndustryJobData>>.Failure(jobs.Error);
    }

    public async Task<Result<IReadOnlyList<IndustryJobData>>> GetCorporationJobsAsync(
        CorporationId corporationId,
        CharacterId authenticatedCharacterId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<EsiIndustryJob>> jobs = await _esiClient
            .GetCorporationIndustryJobsAsync(corporationId, authenticatedCharacterId, cancellationToken)
            .ConfigureAwait(false);

        return jobs.IsSuccess
            ? Result<IReadOnlyList<IndustryJobData>>.Success(jobs.Value.Select(MapJob).ToList())
            : Result<IReadOnlyList<IndustryJobData>>.Failure(jobs.Error);
    }

    private static IndustryJobData MapJob(EsiIndustryJob job) => new(
        job.JobId,
        job.InstallerId,
        job.FacilityId,
        job.LocationId,
        job.ActivityId,
        job.BlueprintId,
        job.BlueprintTypeId,
        job.BlueprintLocationId,
        job.OutputLocationId,
        job.Runs,
        job.Cost,
        job.LicensedRuns,
        job.Probability,
        job.ProductTypeId,
        job.Status,
        job.Duration,
        job.StartDate,
        job.EndDate,
        job.PauseDate,
        job.CompletedDate,
        job.CompletedCharacterId,
        job.SuccessfulRuns,
        job.Scope);
}