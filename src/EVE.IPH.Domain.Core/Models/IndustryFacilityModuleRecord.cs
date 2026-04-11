using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;

namespace EVE.IPH.Domain.Core.Models;

public sealed record IndustryFacilityModuleRecord(
    CharacterId CharacterId,
    FacilityProductionType ProductionType,
    long FacilityId,
    int ModuleTypeId);