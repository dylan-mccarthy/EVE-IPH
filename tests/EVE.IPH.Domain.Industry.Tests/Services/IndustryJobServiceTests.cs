using EVE.IPH.Domain.Industry.Models;
using EVE.IPH.Domain.Industry.Services;

namespace EVE.IPH.Domain.Industry.Tests.Services;

public sealed class IndustryJobServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 11, 12, 0, 0, TimeSpan.Zero);

    private readonly IndustryJobService _service = new();

    [Fact]
    public void GetState_ActiveJobEndingInPast_ReturnsComplete()
    {
        IndustryJob job = CreateJob(1, "active", Now.AddHours(-2), Now.AddMinutes(-1));

        IndustryJobState result = _service.GetState(job, Now);

        result.Should().Be(IndustryJobState.Complete);
    }

    [Fact]
    public void GetState_ActiveJobRunningNow_ReturnsInProgress()
    {
        IndustryJob job = CreateJob(1, "active", Now.AddHours(-1), Now.AddHours(1));

        IndustryJobState result = _service.GetState(job, Now);

        result.Should().Be(IndustryJobState.InProgress);
    }

    [Fact]
    public void GetState_ActiveJobStartingLater_ReturnsPending()
    {
        IndustryJob job = CreateJob(1, "active", Now.AddHours(1), Now.AddHours(3));

        IndustryJobState result = _service.GetState(job, Now);

        result.Should().Be(IndustryJobState.Pending);
    }

    [Theory]
    [InlineData("delivered", IndustryJobState.Completed)]
    [InlineData("cancelled", IndustryJobState.Cancelled)]
    [InlineData("paused", IndustryJobState.Paused)]
    [InlineData("ready", IndustryJobState.Ready)]
    [InlineData("reverted", IndustryJobState.Reverted)]
    [InlineData("mystery", IndustryJobState.Unknown)]
    public void GetState_NonActiveStatuses_MapsLegacyState(string status, IndustryJobState expected)
    {
        IndustryJob job = CreateJob(1, status, null, null);

        IndustryJobState result = _service.GetState(job, Now);

        result.Should().Be(expected);
    }

    [Fact]
    public void SummarizeCurrentJobs_UsesLegacyActivityBucketsAndActiveFilter()
    {
        IndustryJob[] jobs =
        [
            CreateJob(1, "active", Now.AddHours(-1), Now.AddHours(2)),
            CreateJob(8, "active", Now.AddHours(1), Now.AddHours(3)),
            CreateJob(11, "active", Now.AddHours(-3), Now.AddMinutes(-5)),
            CreateJob(9, "active", Now.AddHours(-2), Now.AddHours(2)),
            CreateJob(1, "delivered", Now.AddHours(-4), Now.AddHours(-3)),
        ];

        IndustryJobSummary result = _service.SummarizeCurrentJobs(jobs, Now);

        result.CurrentManufacturingJobs.Should().Be(1);
        result.CurrentResearchJobs.Should().Be(1);
        result.CurrentReactionJobs.Should().Be(2);
        result.PendingJobs.Should().Be(1);
        result.InProgressJobs.Should().Be(2);
        result.CompleteJobs.Should().Be(1);
    }

    [Fact]
    public void GetState_ActiveJobMissingDates_ReturnsUnknown()
    {
        IndustryJob job = CreateJob(1, "active", Now.AddHours(-1), null);

        IndustryJobState result = _service.GetState(job, Now);

        result.Should().Be(IndustryJobState.Unknown);
    }

    private static IndustryJob CreateJob(
        int activityId,
        string status,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate)
    {
        return new IndustryJob(
            JobId: 1001,
            InstallerId: 2002,
            ActivityId: activityId,
            Status: status,
            StartDate: startDate,
            EndDate: endDate);
    }
}