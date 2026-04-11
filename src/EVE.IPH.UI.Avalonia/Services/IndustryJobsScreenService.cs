using EVE.IPH.Domain.Industry.Services;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class IndustryJobsScreenService : IIndustryJobsScreenService
{
    private readonly IPhase11SampleDataProvider _sampleDataProvider;
    private readonly IIndustryJobService _industryJobService;
    private readonly IIndustryJobPresentationService _industryJobPresentationService;

    public IndustryJobsScreenService(
        IPhase11SampleDataProvider sampleDataProvider,
        IIndustryJobService industryJobService,
        IIndustryJobPresentationService industryJobPresentationService)
    {
        _sampleDataProvider = sampleDataProvider ?? throw new ArgumentNullException(nameof(sampleDataProvider));
        _industryJobService = industryJobService ?? throw new ArgumentNullException(nameof(industryJobService));
        _industryJobPresentationService = industryJobPresentationService ?? throw new ArgumentNullException(nameof(industryJobPresentationService));
    }

    public IndustryJobsScreenData GetScreenData(DateTimeOffset now)
    {
        IReadOnlyList<EVE.IPH.Domain.Industry.Models.IndustryJobViewItem> jobs = _sampleDataProvider.GetIndustryJobs();
        EVE.IPH.Domain.Industry.Models.IndustryJobSummary summary = _industryJobService.SummarizeCurrentJobs(jobs.Select(job => job.Job), now);
        IReadOnlyList<EVE.IPH.Domain.Industry.Models.IndustryJobDisplayRow> rows = jobs
            .Select(job => _industryJobPresentationService.Present(job, now))
            .OrderBy(job => job.StateText, StringComparer.OrdinalIgnoreCase)
            .ThenBy(job => job.BlueprintName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new IndustryJobsScreenData(summary, rows);
    }
}