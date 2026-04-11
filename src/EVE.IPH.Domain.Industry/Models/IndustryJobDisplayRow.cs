namespace EVE.IPH.Domain.Industry.Models;

public sealed record IndustryJobDisplayRow(
    long JobId,
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
    string ScopeText,
    IndustryJobState State,
    string StateText,
    string StatusText);