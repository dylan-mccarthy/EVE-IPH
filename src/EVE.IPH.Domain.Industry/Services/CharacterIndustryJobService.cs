using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Industry.Models;

namespace EVE.IPH.Domain.Industry.Services;

public sealed class CharacterIndustryJobService(
    IIndustryJobRepository industryJobRepository,
    IIndustryJobDataSource industryJobDataSource,
    IIndustryJobService industryJobService,
    TimeProvider timeProvider) : ICharacterIndustryJobService
{
    private readonly IIndustryJobRepository _industryJobRepository = industryJobRepository ?? throw new ArgumentNullException(nameof(industryJobRepository));
    private readonly IIndustryJobDataSource _industryJobDataSource = industryJobDataSource ?? throw new ArgumentNullException(nameof(industryJobDataSource));
    private readonly IIndustryJobService _industryJobService = industryJobService ?? throw new ArgumentNullException(nameof(industryJobService));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<Result<IndustryJobSnapshot>> GetAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<IndustryJobRecord>> storedJobs = await _industryJobRepository
            .GetByInstallerIdAsync(characterId, cancellationToken)
            .ConfigureAwait(false);

        if (storedJobs.IsFailure)
        {
            return Result<IndustryJobSnapshot>.Failure(storedJobs.Error);
        }

        return CreateSnapshot(characterId, storedJobs.Value);
    }

    public async Task<Result<IndustryJobSnapshot>> RefreshAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<IndustryJobData>> currentJobs = await _industryJobDataSource
            .GetCharacterJobsAsync(characterId, cancellationToken)
            .ConfigureAwait(false);

        if (currentJobs.IsFailure)
        {
            return Result<IndustryJobSnapshot>.Failure(currentJobs.Error);
        }

        IReadOnlyList<IndustryJobRecord> normalizedJobs = currentJobs.Value
            .Select(job => new IndustryJobRecord(
                job.JobId,
                characterId,
                job.FacilityId,
                job.LocationId,
                NormalizeActivityId(job.ActivityId),
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
                job.Scope))
            .ToArray();

        Result<IReadOnlyList<IndustryJobRecord>> storedJobs = await _industryJobRepository
            .ReplaceAsync(characterId, IndustryJobScope.Personal, normalizedJobs, cancellationToken)
            .ConfigureAwait(false);

        if (storedJobs.IsFailure)
        {
            return Result<IndustryJobSnapshot>.Failure(storedJobs.Error);
        }

        return CreateSnapshot(characterId, storedJobs.Value);
    }

    private Result<IndustryJobSnapshot> CreateSnapshot(
        CharacterId characterId,
        IReadOnlyList<IndustryJobRecord> records)
    {
        IReadOnlyList<IndustryJob> jobs = records
            .Select(record => new IndustryJob(
                record.JobId,
                record.InstallerId.Value,
                record.ActivityId,
                record.Status,
                record.StartDate,
                record.EndDate))
            .ToArray();

        IndustryJobSummary summary = _industryJobService.SummarizeCurrentJobs(jobs, _timeProvider.GetUtcNow());

        return Result<IndustryJobSnapshot>.Success(new IndustryJobSnapshot(characterId, jobs, summary));
    }

    private static int NormalizeActivityId(int activityId) => activityId == 9 ? 11 : activityId;
}