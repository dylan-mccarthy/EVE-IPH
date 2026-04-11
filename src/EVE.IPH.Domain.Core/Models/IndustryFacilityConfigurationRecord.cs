using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Models;

public sealed record IndustryFacilityConfigurationRecord(
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
    Maybe<double> MaterialMultiplierOverride,
    Maybe<double> TimeMultiplierOverride,
    Maybe<double> CostMultiplierOverride);