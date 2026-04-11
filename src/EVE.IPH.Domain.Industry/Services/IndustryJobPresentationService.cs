using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Industry.Models;

namespace EVE.IPH.Domain.Industry.Services;

public sealed class IndustryJobPresentationService(IIndustryJobService industryJobService) : IIndustryJobPresentationService
{
    private readonly IIndustryJobService _industryJobService = industryJobService ?? throw new ArgumentNullException(nameof(industryJobService));

    public IndustryJobDisplayRow Present(IndustryJobViewItem job, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(job);

        IndustryJobState state = _industryJobService.GetState(job.Job, now);

        return new IndustryJobDisplayRow(
            job.Job.JobId,
            job.InstallerName,
            job.ActivityName,
            job.BlueprintName,
            job.OutputItemName,
            job.OutputItemType,
            job.InstallSystem,
            job.InstallRegion,
            job.LicensedRuns,
            job.Runs,
            job.SuccessfulRuns,
            job.BlueprintLocation,
            job.OutputLocation,
            job.Scope == IndustryJobScope.Corporation ? "Corporation" : "Personal",
            state,
            FormatStateText(state),
            FormatStatusText(job.Job.Status, state));
    }

    private static string FormatStateText(IndustryJobState state) => state switch
    {
        IndustryJobState.InProgress => "In Progress",
        IndustryJobState.Completed => "Completed",
        IndustryJobState.Cancelled => "Canceled",
        _ => state.ToString(),
    };

    private static string FormatStatusText(string status, IndustryJobState state)
    {
        if (status.Equals("active", StringComparison.OrdinalIgnoreCase))
        {
            return "Delivered";
        }

        return state == IndustryJobState.Completed
            ? "Ready for Delivery"
            : "In Progress";
    }
}