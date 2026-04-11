using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.UI.Avalonia.Services;
using EVE.IPH.UI.Avalonia.ViewModels;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.ViewModels;

public sealed class ResearchAgentsViewModelTests
{
    [Fact]
    public async Task Constructor_WhenLoadSucceeds_PopulatesSummaryAndStatus()
    {
        IResearchAgentsScreenService screenService = Substitute.For<IResearchAgentsScreenService>();
        ResearchAgentsScreenData screenData = new(
            new ResearchAgentDatacoreSummary(
                [new ResearchAgentDatacoreSnapshot("Arajna Yashar", "Mechanical Engineering", "Datacore - Mechanical Engineering", 1860, 18, 2790000, 115.5, 4, "Rens")],
                2790000),
            "Loaded research agents for Kara Maken.");
        screenService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(screenData));

        ResearchAgentsViewModel viewModel = new(screenService);
        await viewModel.LoadTask;

        viewModel.Agents.Should().HaveCount(1);
        viewModel.StatusText.Should().Be("Loaded research agents for Kara Maken.");
        viewModel.SummaryText.Should().Contain("1 active research agents");
    }

    [Fact]
    public async Task Constructor_WhenLoadFails_ExposesErrorStatus()
    {
        IResearchAgentsScreenService screenService = Substitute.For<IResearchAgentsScreenService>();
        screenService.GetScreenDataAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ResearchAgentsScreenData>(new InvalidOperationException("load failed")));

        ResearchAgentsViewModel viewModel = new(screenService);
        await viewModel.LoadTask;

        viewModel.Agents.Should().BeEmpty();
        viewModel.StatusText.Should().Be("Unable to load research agents: load failed");
    }

    [Fact]
    public async Task RefreshAsync_WhenCalled_ReloadsScreenData()
    {
        IResearchAgentsScreenService screenService = Substitute.For<IResearchAgentsScreenService>();
        screenService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(
            Task.FromResult(new ResearchAgentsScreenData(new ResearchAgentDatacoreSummary([], 0), "Initial")),
            Task.FromResult(new ResearchAgentsScreenData(
                new ResearchAgentDatacoreSummary(
                    [new ResearchAgentDatacoreSnapshot("Arajna Yashar", "Mechanical Engineering", "Datacore - Mechanical Engineering", 1860, 18, 2790000, 115.5, 4, "Rens")],
                    2790000),
                "Reloaded")));

        ResearchAgentsViewModel viewModel = new(screenService);
        await viewModel.LoadTask;

        await viewModel.RefreshAsync();

        viewModel.Agents.Should().HaveCount(1);
        viewModel.StatusText.Should().Be("Reloaded");
        viewModel.CanRefresh.Should().BeTrue();
    }
}