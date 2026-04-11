using EVE.IPH.Domain.Industry.Services;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class IndustryJobsScreenService : IIndustryJobsScreenService
{
    private readonly IIndustryJobService _industryJobService;
    private readonly IIndustryJobPresentationService _industryJobPresentationService;
    private readonly IIndustryJobReadRepository _industryJobReadRepository;

    public IndustryJobsScreenService(
        IIndustryJobService industryJobService,
        IIndustryJobPresentationService industryJobPresentationService,
        IIndustryJobReadRepository industryJobReadRepository)
    {
        _industryJobService = industryJobService ?? throw new ArgumentNullException(nameof(industryJobService));
        _industryJobPresentationService = industryJobPresentationService ?? throw new ArgumentNullException(nameof(industryJobPresentationService));
        _industryJobReadRepository = industryJobReadRepository ?? throw new ArgumentNullException(nameof(industryJobReadRepository));
    }

    public IndustryJobsScreenData GetScreenData(DateTimeOffset now) =>
        BuildScreenData(MapJobs(_industryJobReadRepository.GetViewRecordsAsync().GetAwaiter().GetResult()), now);

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

    private IndustryJobsScreenData BuildScreenData(IReadOnlyList<EVE.IPH.Domain.Industry.Models.IndustryJobViewItem> jobs, DateTimeOffset now)
    {
        EVE.IPH.Domain.Industry.Models.IndustryJobSummary summary = _industryJobService.SummarizeCurrentJobs(jobs.Select(job => job.Job), now);
        IReadOnlyList<EVE.IPH.Domain.Industry.Models.IndustryJobDisplayRow> rows = jobs
            .Select(job => _industryJobPresentationService.Present(job, now))
            .OrderBy(job => job.StateText, StringComparer.OrdinalIgnoreCase)
            .ThenBy(job => job.BlueprintName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new IndustryJobsScreenData(summary, rows);
    }

}