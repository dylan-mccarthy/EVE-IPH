using CommunityToolkit.Mvvm.ComponentModel;
using EVE.IPH.Domain.Industry.Models;
using EVE.IPH.Domain.Industry.Services;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class IndustryJobsViewModel : ObservableObject
{
    private readonly IIndustryJobsScreenService _industryJobsScreenService;
    private IndustryJobSummary _summary = new(0, 0, 0, 0, 0, 0);
    private IReadOnlyList<IndustryJobDisplayRow> _items = [];
    private bool _isRefreshing;

    public IndustryJobsViewModel(IIndustryJobsScreenService industryJobsScreenService)
    {
        _industryJobsScreenService = industryJobsScreenService ?? throw new ArgumentNullException(nameof(industryJobsScreenService));

        Refresh();
    }

    public IndustryJobSummary Summary
    {
        get => _summary;
        private set
        {
            if (SetProperty(ref _summary, value))
            {
                OnPropertyChanged(nameof(SummaryText));
            }
        }
    }

    public IReadOnlyList<IndustryJobDisplayRow> Items
    {
        get => _items;
        private set
        {
            if (SetProperty(ref _items, value))
            {
                OnPropertyChanged(nameof(SummaryText));
            }
        }
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set
        {
            if (SetProperty(ref _isRefreshing, value))
            {
                OnPropertyChanged(nameof(CanRefresh));
            }
        }
    }

    public bool CanRefresh => !IsRefreshing;

    public string SummaryText =>
        Items.Count == 0
            ? "No synced industry jobs were found yet. Current jobs will appear here after the missing industry-job repository and ESI refresh seams are wired."
            : $"Manufacturing: {Summary.CurrentManufacturingJobs} | Research: {Summary.CurrentResearchJobs} | Reactions: {Summary.CurrentReactionJobs} | Pending: {Summary.PendingJobs} | In Progress: {Summary.InProgressJobs} | Complete: {Summary.CompleteJobs}";

    public void Refresh()
    {
        if (IsRefreshing)
        {
            return;
        }

        try
        {
            IsRefreshing = true;
            IndustryJobsScreenData screenData = _industryJobsScreenService.GetScreenData(DateTimeOffset.UtcNow);
            Summary = screenData.Summary;
            Items = screenData.Rows;
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}