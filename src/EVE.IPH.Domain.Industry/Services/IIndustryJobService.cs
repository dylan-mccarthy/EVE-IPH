using EVE.IPH.Domain.Industry.Models;

namespace EVE.IPH.Domain.Industry.Services;

public interface IIndustryJobService
{
    IndustryJobState GetState(IndustryJob job, DateTimeOffset now);

    IndustryJobSummary SummarizeCurrentJobs(IEnumerable<IndustryJob> jobs, DateTimeOffset now);
}