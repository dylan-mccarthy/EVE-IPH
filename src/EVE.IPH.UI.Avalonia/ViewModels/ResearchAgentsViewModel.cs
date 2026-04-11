using CommunityToolkit.Mvvm.ComponentModel;
using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.UI.Avalonia.Services;

namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class ResearchAgentsViewModel : ObservableObject
{
    private ResearchAgentDatacoreSummary _summary = new([], 0);
    private string _statusText = "Loading research agents...";

    public ResearchAgentsViewModel(IResearchAgentsScreenService researchAgentsScreenService)
    {
        ArgumentNullException.ThrowIfNull(researchAgentsScreenService);

        LoadTask = LoadAsync(researchAgentsScreenService);
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

    public string SummaryText => $"{Summary.Agents.Count} active research agents | Estimated datacore value: {Summary.TotalValue:F2} ISK";

    private async Task LoadAsync(IResearchAgentsScreenService researchAgentsScreenService)
    {
        try
        {
            ResearchAgentsScreenData screenData = await researchAgentsScreenService
                .GetScreenDataAsync()
                .ConfigureAwait(false);

            Summary = screenData.Summary;
            StatusText = screenData.StatusText;
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to load research agents: {ex.Message}";
        }
    }
}