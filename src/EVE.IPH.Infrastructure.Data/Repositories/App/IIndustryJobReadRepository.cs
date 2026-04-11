using EVE.IPH.Domain.Core.Interfaces;

namespace EVE.IPH.Infrastructure.Data.Repositories.App;

public interface IIndustryJobReadRepository
{
    Task<EVE.IPH.Domain.Core.Results.Result<IReadOnlyList<IndustryJobScreenRecord>>> GetViewRecordsAsync(CancellationToken cancellationToken = default);
}

public sealed record IndustryJobScreenRecord(
    long JobId,
    long InstallerId,
    int ActivityId,
    string Status,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    string InstallerName,
    string ActivityName,
    string BlueprintName,
    string OutputItemName,
    string OutputItemType,
    string InstallSystem,
    string InstallRegion,
    int LicensedRuns,
    long Runs,
    int SuccessfulRuns,
    string BlueprintLocation,
    string OutputLocation,
    IndustryJobScope Scope);