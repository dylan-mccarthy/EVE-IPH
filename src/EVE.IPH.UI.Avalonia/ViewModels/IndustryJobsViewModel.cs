using CommunityToolkit.Mvvm.ComponentModel;
using EVE.IPH.Domain.Industry.Models;
using EVE.IPH.Domain.Industry.Services;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class IndustryJobsViewModel : ObservableObject
{
    private readonly IIndustryJobsQueryService _industryJobsQueryService;
    private readonly IIndustryJobsCommandService _industryJobsCommandService;
    private IndustryJobSummary _summary = new(0, 0, 0, 0, 0, 0);
    private IReadOnlyList<IndustryJobDisplayRow> _items = [];
    private string _statusText = "Loading industry jobs...";
    private bool _isRefreshing;

    public IndustryJobsViewModel(
        IIndustryJobsQueryService industryJobsQueryService,
        IIndustryJobsCommandService industryJobsCommandService)
    {
        _industryJobsQueryService = industryJobsQueryService ?? throw new ArgumentNullException(nameof(industryJobsQueryService));
        _industryJobsCommandService = industryJobsCommandService ?? throw new ArgumentNullException(nameof(industryJobsCommandService));

        LoadTask = RefreshAsync(loadOnly: true);
    }

    public Task LoadTask { get; }

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

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (SetProperty(ref _statusText, value))
            {
                OnPropertyChanged(nameof(SummaryText));
            }
        }
    }

    public string SummaryText =>
        Items.Count == 0
            ? StatusText
            : $"Manufacturing: {Summary.CurrentManufacturingJobs} | Research: {Summary.CurrentResearchJobs} | Reactions: {Summary.CurrentReactionJobs} | Pending: {Summary.PendingJobs} | In Progress: {Summary.InProgressJobs} | Complete: {Summary.CompleteJobs}";

    public Task RefreshAsync() => RefreshAsync(loadOnly: false);

    private async Task RefreshAsync(bool loadOnly)
    {
        if (IsRefreshing)
        {
            return;
        }

        try
        {
            IsRefreshing = true;
            IndustryJobsScreenData screenData = loadOnly
                ? await _industryJobsQueryService.GetScreenDataAsync(DateTimeOffset.UtcNow).ConfigureAwait(false)
                : await _industryJobsCommandService.RefreshAsync(DateTimeOffset.UtcNow).ConfigureAwait(false);
            Summary = screenData.Summary;
            Items = screenData.Rows;
            StatusText = screenData.StatusText;
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}