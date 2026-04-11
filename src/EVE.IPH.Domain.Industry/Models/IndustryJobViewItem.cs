using EVE.IPH.Domain.Core.Interfaces;

namespace EVE.IPH.Domain.Industry.Models;

public sealed record IndustryJobViewItem(
    IndustryJob Job,
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