namespace EVE.IPH.UI.Avalonia.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel(
        AssetsViewModel assets,
        IndustryJobsViewModel industryJobs,
        ResearchAgentsViewModel researchAgents)
    {
        Assets = assets ?? throw new ArgumentNullException(nameof(assets));
        IndustryJobs = industryJobs ?? throw new ArgumentNullException(nameof(industryJobs));
        ResearchAgents = researchAgents ?? throw new ArgumentNullException(nameof(researchAgents));
    }

    public string Title => "EVE IPH Modern";

    public string Subtitle => "Phase 11 host bootstrap with DI-backed read-only screens over the extracted Phase 10 domain services.";

    public AssetsViewModel Assets { get; }

    public IndustryJobsViewModel IndustryJobs { get; }

    public ResearchAgentsViewModel ResearchAgents { get; }
}