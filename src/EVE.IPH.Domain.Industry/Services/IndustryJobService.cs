using EVE.IPH.Domain.Industry.Models;

namespace EVE.IPH.Domain.Industry.Services;

public sealed class IndustryJobService : IIndustryJobService
{
    public IndustryJobState GetState(IndustryJob job, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (job.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
        {
            if (job.EndDate is null || job.StartDate is null)
            {
                return IndustryJobState.Unknown;
            }

            if (job.EndDate.Value <= now)
            {
                return IndustryJobState.Complete;
            }

            if (job.StartDate.Value <= now)
            {
                return IndustryJobState.InProgress;
            }

            return IndustryJobState.Pending;
        }

        return job.Status.ToLowerInvariant() switch
        {
            "delivered" => IndustryJobState.Completed,
            "cancelled" => IndustryJobState.Cancelled,
            "paused" => IndustryJobState.Paused,
            "ready" => IndustryJobState.Ready,
            "reverted" => IndustryJobState.Reverted,
            _ => IndustryJobState.Unknown,
        };
    }

    public IndustryJobSummary SummarizeCurrentJobs(IEnumerable<IndustryJob> jobs, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(jobs);

        int manufacturingJobs = 0;
        int researchJobs = 0;
        int reactionJobs = 0;
        int pendingJobs = 0;
        int inProgressJobs = 0;
        int completeJobs = 0;

        foreach (IndustryJob job in jobs)
        {
            ArgumentNullException.ThrowIfNull(job);

            if (!job.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (NormalizeActivityId(job.ActivityId))
            {
                case 1:
                    manufacturingJobs++;
                    break;
                case 11:
                    reactionJobs++;
                    break;
                default:
                    researchJobs++;
                    break;
            }

            switch (GetState(job, now))
            {
                case IndustryJobState.Pending:
                    pendingJobs++;
                    break;
                case IndustryJobState.InProgress:
                    inProgressJobs++;
                    break;
                case IndustryJobState.Complete:
                    completeJobs++;
                    break;
            }
        }

        return new IndustryJobSummary(
            manufacturingJobs,
            researchJobs,
            reactionJobs,
            pendingJobs,
            inProgressJobs,
            completeJobs);
    }

    private static int NormalizeActivityId(int activityId) => activityId == 9 ? 11 : activityId;
}