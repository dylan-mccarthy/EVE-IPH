namespace EVE.IPH.Domain.Industry.Models;

public sealed record IndustryJobSummary(
    int CurrentManufacturingJobs,
    int CurrentResearchJobs,
    int CurrentReactionJobs,
    int PendingJobs,
    int InProgressJobs,
    int CompleteJobs);