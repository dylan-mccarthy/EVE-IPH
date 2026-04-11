using EVE.IPH.Domain.Industry.Services;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class IndustryJobsQueryService : IIndustryJobsQueryService
{
    private readonly IIndustryJobService _industryJobService;
    private readonly IIndustryJobPresentationService _industryJobPresentationService;
    private readonly IIndustryJobReadRepository _industryJobReadRepository;

    public IndustryJobsQueryService(
        IIndustryJobService industryJobService,
        IIndustryJobPresentationService industryJobPresentationService,
        IIndustryJobReadRepository industryJobReadRepository)
    {
        _industryJobService = industryJobService ?? throw new ArgumentNullException(nameof(industryJobService));
        _industryJobPresentationService = industryJobPresentationService ?? throw new ArgumentNullException(nameof(industryJobPresentationService));
        _industryJobReadRepository = industryJobReadRepository ?? throw new ArgumentNullException(nameof(industryJobReadRepository));
    }

    public async Task<IndustryJobsScreenData> GetScreenDataAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<IndustryJobScreenRecord>> jobsResult = await _industryJobReadRepository
            .GetViewRecordsAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<EVE.IPH.Domain.Industry.Models.IndustryJobViewItem> jobs = MapJobs(jobsResult);
        return BuildScreenData(jobs, now, BuildStatusText(jobsResult, jobs.Count));
    }

    private static IReadOnlyList<EVE.IPH.Domain.Industry.Models.IndustryJobViewItem> MapJobs(Result<IReadOnlyList<IndustryJobScreenRecord>> result)
    {
        if (result.IsFailure)
        {
            return [];
        }

        return result.Value
            .Select(job => new EVE.IPH.Domain.Industry.Models.IndustryJobViewItem(
                new EVE.IPH.Domain.Industry.Models.IndustryJob(
                    job.JobId,
                    job.InstallerId,
                    job.ActivityId,
                    job.Status,
                    job.StartDate,
                    job.EndDate),
                job.InstallerName,
                job.ActivityName,
                job.BlueprintName,
                job.OutputItemName,
                job.OutputItemType,
                job.InstallSystem,
                job.InstallRegion,
                job.LicensedRuns,
                job.Runs,
                job.SuccessfulRuns,
                job.BlueprintLocation,
                job.OutputLocation,
                job.Scope))
            .ToArray();
    }

    private IndustryJobsScreenData BuildScreenData(IReadOnlyList<EVE.IPH.Domain.Industry.Models.IndustryJobViewItem> jobs, DateTimeOffset now, string statusText)
    {
        EVE.IPH.Domain.Industry.Models.IndustryJobSummary summary = _industryJobService.SummarizeCurrentJobs(jobs.Select(job => job.Job), now);
        IReadOnlyList<EVE.IPH.Domain.Industry.Models.IndustryJobDisplayRow> rows = jobs
            .Select(job => _industryJobPresentationService.Present(job, now))
            .OrderBy(job => job.StateText, StringComparer.OrdinalIgnoreCase)
            .ThenBy(job => job.BlueprintName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new IndustryJobsScreenData(summary, rows, statusText);
    }

    private static string BuildStatusText(Result<IReadOnlyList<IndustryJobScreenRecord>> jobsResult, int rowCount)
    {
        if (jobsResult.IsFailure)
        {
            return $"Unable to load synced industry jobs: {jobsResult.Error.Message}";
        }

        return rowCount == 0
            ? "No synced industry jobs were found yet. Use Refresh Industry Jobs to pull the latest character and corporation jobs from ESI."
            : "Loaded synced industry-job records from the local SQLite store.";
    }
}