using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IStructureFacilityManagementQueryService
{
    Task<StructureFacilityManagementScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default);
}

public sealed record StructureFacilityManagementScreenData(
    IReadOnlyList<StructureFacilityCharacterOption> Characters,
    IReadOnlyList<IndustryStructureRow> Structures,
    IReadOnlyList<FacilitySettingsRow> Facilities,
    IReadOnlyList<FacilityProductionTypeOption> ProductionTypes,
    IReadOnlyList<FacilityKindOption> FacilityKinds,
    string StatusText);

public sealed record StructureFacilityCharacterOption(
    CharacterId CharacterId,
    string Name,
    bool IsDefault);

public sealed record IndustryStructureRow(
    long StructureId,
    string StructureName,
    int StructureTypeId,
    long SolarSystemId,
    long RegionId,
    long? OwnerCorporationId,
    bool IsManualEntry,
    DateTimeOffset UpdatedAtUtc)
{
    public string SummaryText => $"Type {StructureTypeId}  Solar System {SolarSystemId}  Region {RegionId}";
}

public sealed record FacilitySettingsRow(
    CharacterId CharacterId,
    string CharacterName,
    FacilityProductionType ProductionType,
    long FacilityId,
    string FacilityName,
    IndustryFacilityKind FacilityKind,
    long? StructureId,
    string? StructureName,
    long RegionId,
    string RegionName,
    long SolarSystemId,
    string SolarSystemName,
    double SolarSystemSecurity,
    double CostIndex,
    double ActivityCostPerSecond,
    bool IncludeActivityCost,
    bool IncludeActivityTime,
    bool IncludeActivityUsage,
    bool ConvertToOre,
    int FactionWarfareUpgradeLevel,
    double TaxRate,
    double? MaterialMultiplierOverride,
    double? TimeMultiplierOverride,
    double? CostMultiplierOverride,
    IReadOnlyList<int> InstalledModuleTypeIds)
{
    public string DisplayName => $"{CharacterName}: {ProductionTypeDisplayName}";

    public string ProductionTypeDisplayName => ProductionType switch
    {
        FacilityProductionType.ComponentManufacturing => "Component Manufacturing",
        FacilityProductionType.CapitalComponentManufacturing => "Capital Component Manufacturing",
        FacilityProductionType.CapitalManufacturing => "Capital Manufacturing",
        FacilityProductionType.SuperManufacturing => "Super Manufacturing",
        FacilityProductionType.T3CruiserManufacturing => "T3 Cruiser Manufacturing",
        FacilityProductionType.T3DestroyerManufacturing => "T3 Destroyer Manufacturing",
        FacilityProductionType.SubsystemManufacturing => "Subsystem Manufacturing",
        FacilityProductionType.BoosterManufacturing => "Booster Manufacturing",
        FacilityProductionType.T3Invention => "T3 Invention",
        _ => ProductionType.ToString(),
    };

    public string ModuleSummary => InstalledModuleTypeIds.Count == 0
        ? "No installed modules"
        : $"Modules: {string.Join(", ", InstalledModuleTypeIds)}";
}

public sealed record FacilityProductionTypeOption(FacilityProductionType ProductionType, string DisplayName);

public sealed record FacilityKindOption(IndustryFacilityKind FacilityKind, string DisplayName);