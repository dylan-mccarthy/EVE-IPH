using EVE.IPH.Domain.Industry.Models;
using EVE.IPH.Domain.Industry.Services;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class IndustryJobsViewModel
{
    public IndustryJobsViewModel(IIndustryJobsScreenService industryJobsScreenService)
    {
        ArgumentNullException.ThrowIfNull(industryJobsScreenService);

        IndustryJobsScreenData screenData = industryJobsScreenService.GetScreenData(DateTimeOffset.UtcNow);
        Summary = screenData.Summary;
        Items = screenData.Rows;
    }

    public IndustryJobSummary Summary { get; }

    public IReadOnlyList<IndustryJobDisplayRow> Items { get; }

    public string SummaryText =>
        $"Manufacturing: {Summary.CurrentManufacturingJobs} | Research: {Summary.CurrentResearchJobs} | Reactions: {Summary.CurrentReactionJobs} | Pending: {Summary.PendingJobs} | In Progress: {Summary.InProgressJobs} | Complete: {Summary.CompleteJobs}";
}