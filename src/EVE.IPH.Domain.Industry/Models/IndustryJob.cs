namespace EVE.IPH.Domain.Industry.Models;

public sealed record IndustryJob(
    long JobId,
    long InstallerId,
    int ActivityId,
    string Status,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate);