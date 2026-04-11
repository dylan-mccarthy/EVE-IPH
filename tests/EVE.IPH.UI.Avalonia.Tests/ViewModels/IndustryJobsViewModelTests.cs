using EVE.IPH.Domain.Industry.Models;
using EVE.IPH.UI.Avalonia.Services;
using EVE.IPH.UI.Avalonia.ViewModels;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.ViewModels;

public sealed class IndustryJobsViewModelTests
{
    [Fact]
    public void Constructor_LoadsSummaryAndRows()
    {
        IIndustryJobsScreenService screenService = Substitute.For<IIndustryJobsScreenService>();
        IndustryJobsScreenData screenData = new(
            new IndustryJobSummary(2, 1, 3, 4, 5, 6),
            [
                new IndustryJobDisplayRow(900001, "Kara Maken", "Manufacturing", "Vargur Blueprint", "Vargur", "Ship", "Jita", "The Forge", 1, 2, 0, "Tatara Alpha", "Ship Hangar", "Personal", IndustryJobState.InProgress, "In Progress", "Runs 0/2")
            ]);
        screenService.GetScreenData(Arg.Any<DateTimeOffset>()).Returns(screenData);

        IndustryJobsViewModel viewModel = new(screenService);

        viewModel.Summary.Should().Be(screenData.Summary);
        viewModel.Items.Should().BeEquivalentTo(screenData.Rows);
        viewModel.SummaryText.Should().Contain("Manufacturing: 2");
        viewModel.SummaryText.Should().Contain("Reactions: 3");
    }

    [Fact]
    public void Refresh_WhenCalled_ReloadsSummaryAndRows()
    {
        IIndustryJobsScreenService screenService = Substitute.For<IIndustryJobsScreenService>();
        screenService.GetScreenData(Arg.Any<DateTimeOffset>()).Returns(
            new IndustryJobsScreenData(new IndustryJobSummary(0, 0, 0, 0, 0, 0), []),
            new IndustryJobsScreenData(
                new IndustryJobSummary(2, 1, 3, 4, 5, 6),
                [new IndustryJobDisplayRow(900001, "Kara Maken", "Manufacturing", "Vargur Blueprint", "Vargur", "Ship", "Jita", "The Forge", 1, 2, 0, "Tatara Alpha", "Ship Hangar", "Personal", IndustryJobState.InProgress, "In Progress", "Runs 0/2") ]));

        IndustryJobsViewModel viewModel = new(screenService);

        viewModel.Refresh();

        viewModel.Items.Should().ContainSingle();
        viewModel.Summary.CurrentManufacturingJobs.Should().Be(2);
        viewModel.CanRefresh.Should().BeTrue();
    }
}