using EVE.IPH.Domain.Industry.Models;
using EVE.IPH.UI.Avalonia.Services;
using EVE.IPH.UI.Avalonia.ViewModels;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.ViewModels;

public sealed class IndustryJobsViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsSummaryAndRows()
    {
        IIndustryJobsQueryService queryService = Substitute.For<IIndustryJobsQueryService>();
        IIndustryJobsCommandService commandService = Substitute.For<IIndustryJobsCommandService>();
        IndustryJobsScreenData screenData = new(
            new IndustryJobSummary(2, 1, 3, 4, 5, 6),
            [
                new IndustryJobDisplayRow(900001, "Kara Maken", "Manufacturing", "Vargur Blueprint", "Vargur", "Ship", "Jita", "The Forge", 1, 2, 0, "Tatara Alpha", "Ship Hangar", "Personal", IndustryJobState.InProgress, "In Progress", "Runs 0/2")
            ],
            "Loaded synced industry-job records from the local SQLite store.");
        queryService.GetScreenDataAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(screenData);

        IndustryJobsViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        viewModel.Summary.Should().Be(screenData.Summary);
        viewModel.Items.Should().BeEquivalentTo(screenData.Rows);
        viewModel.SummaryText.Should().Contain("Manufacturing: 2");
        viewModel.SummaryText.Should().Contain("Reactions: 3");
    }

    [Fact]
    public async Task RefreshAsync_WhenCalled_ReloadsSummaryAndRows()
    {
        IIndustryJobsQueryService queryService = Substitute.For<IIndustryJobsQueryService>();
        IIndustryJobsCommandService commandService = Substitute.For<IIndustryJobsCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(
            new IndustryJobsScreenData(new IndustryJobSummary(0, 0, 0, 0, 0, 0), [], "No synced industry jobs were found yet."));
        commandService.RefreshAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(
            new IndustryJobsScreenData(
                new IndustryJobSummary(2, 1, 3, 4, 5, 6),
                [new IndustryJobDisplayRow(900001, "Kara Maken", "Manufacturing", "Vargur Blueprint", "Vargur", "Ship", "Jita", "The Forge", 1, 2, 0, "Tatara Alpha", "Ship Hangar", "Personal", IndustryJobState.InProgress, "In Progress", "Runs 0/2") ],
                "Refreshed industry jobs for 1 connected character."));

        IndustryJobsViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        await viewModel.RefreshAsync();

        viewModel.Items.Should().ContainSingle();
        viewModel.Summary.CurrentManufacturingJobs.Should().Be(2);
        viewModel.CanRefresh.Should().BeTrue();
    }
}