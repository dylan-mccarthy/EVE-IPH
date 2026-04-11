using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Industry.Models;
using EVE.IPH.Domain.Industry.Services;

namespace EVE.IPH.Domain.Industry.Tests.Services;

public sealed class IndustryJobPresentationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Present_MapsActiveCorporationJobToDisplayRow()
    {
        IndustryJobPresentationService service = new(new IndustryJobService());
        IndustryJobViewItem job = new(
            new IndustryJob(1001, 90000001, 1, "active", Now.AddHours(-1), Now.AddHours(1)),
            "Builder One",
            "Manufacturing",
            "Hulk Blueprint",
            "Hulk",
            "Ship",
            "Jita",
            "The Forge",
            5,
            2,
            0,
            "Hangar A",
            "Hangar B",
            IndustryJobScope.Corporation);

        IndustryJobDisplayRow result = service.Present(job, Now);

        result.ScopeText.Should().Be("Corporation");
        result.State.Should().Be(IndustryJobState.InProgress);
        result.StateText.Should().Be("In Progress");
        result.StatusText.Should().Be("Delivered");
    }

    [Fact]
    public void Present_MapsDeliveredJobToReadyForDeliveryStatus()
    {
        IndustryJobPresentationService service = new(new IndustryJobService());
        IndustryJobViewItem job = new(
            new IndustryJob(1002, 90000002, 8, "delivered", Now.AddHours(-3), Now.AddHours(-1)),
            "Researcher Two",
            "Research Time",
            "Drake Blueprint",
            "",
            "",
            "Amarr",
            "Domain",
            1,
            1,
            1,
            "Lab",
            "Lab",
            IndustryJobScope.Personal);

        IndustryJobDisplayRow result = service.Present(job, Now);

        result.ScopeText.Should().Be("Personal");
        result.State.Should().Be(IndustryJobState.Completed);
        result.StateText.Should().Be("Completed");
        result.StatusText.Should().Be("Ready for Delivery");
    }
}