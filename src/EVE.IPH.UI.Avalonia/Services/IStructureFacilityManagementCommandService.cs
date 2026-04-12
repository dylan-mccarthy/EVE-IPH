using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Models;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IStructureFacilityManagementCommandService
{
    Task<Result<IndustryStructureRecord>> SaveStructureAsync(StructureUpsertRequest request, CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteStructureAsync(long structureId, CancellationToken cancellationToken = default);

    Task<Result<IndustryFacilityConfigurationRecord>> SaveFacilityAsync(FacilitySettingsUpsertRequest request, CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteFacilityAsync(CharacterId characterId, FacilityProductionType productionType, CancellationToken cancellationToken = default);
}

public sealed record StructureUpsertRequest(
    long StructureId,
    string StructureName,
    int StructureTypeId,
    long SolarSystemId,
    long RegionId,
    long? OwnerCorporationId,
    bool IsManualEntry);

public sealed record FacilitySettingsUpsertRequest(
    CharacterId CharacterId,
    FacilityProductionType ProductionType,
    long FacilityId,
    string FacilityName,
    IndustryFacilityKind FacilityKind,
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
    IReadOnlyList<int> ModuleTypeIds);