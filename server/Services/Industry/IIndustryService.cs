namespace server.Services.Industry;

public interface IIndustryService
{
    Task<List<IndustryJob>> GetIndustryJobsAsync(long characterId, bool includeCompleted = true, CancellationToken ct = default);
}

public sealed record IndustryJob(
    int JobId,
    int InstallerId,
    long FacilityId,
    long LocationId,
    int ActivityId,
    int BlueprintId,
    long BlueprintTypeId,
    long BlueprintLocationId,
    long OutputLocationId,
    int Runs,
    decimal Cost,
    int LicensedRuns,
    double Probability,
    int? ProductTypeId,
    string Status,
    int TimeInSeconds,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    DateTimeOffset? PauseDate,
    DateTimeOffset? CompletedDate,
    int? CompletedCharacterId,
    int? SuccessfulRuns
);
