using CommunityToolkit.Mvvm.ComponentModel;
using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class ResearchAgentsViewModel : ObservableObject
{
    private readonly IResearchAgentsScreenService _researchAgentsScreenService;
    private ResearchAgentDatacoreSummary _summary = new([], 0);
    private string _statusText = "Loading research agents...";
    private bool _isRefreshing;

    public ResearchAgentsViewModel(IResearchAgentsScreenService researchAgentsScreenService)
    {
        _researchAgentsScreenService = researchAgentsScreenService ?? throw new ArgumentNullException(nameof(researchAgentsScreenService));

        LoadTask = RefreshAsync();
    }

    public Task LoadTask { get; }

    public ResearchAgentDatacoreSummary Summary
    {
        get => _summary;
        private set
        {
            if (SetProperty(ref _summary, value))
            {
                OnPropertyChanged(nameof(Agents));
                OnPropertyChanged(nameof(SummaryText));
            }
        }
    }

    public IReadOnlyList<ResearchAgentDatacoreSnapshot> Agents => Summary.Agents;

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
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

    public string SummaryText => $"{Summary.Agents.Count} active research agents | Estimated datacore value: {Summary.TotalValue:F2} ISK";

    public async Task RefreshAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        try
        {
            IsRefreshing = true;

            ResearchAgentsScreenData screenData = await _researchAgentsScreenService
                .GetScreenDataAsync()
                .ConfigureAwait(false);

            Summary = screenData.Summary;
            StatusText = screenData.StatusText;
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to load research agents: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}