using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Models;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Manufacturing.Services;

public interface IManufacturingFacilityConfigurationService
{
    Task<Result<IReadOnlyList<ResolvedIndustryFacilityConfiguration>>> GetFacilitiesAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    Task<Result<Maybe<ResolvedIndustryFacilityConfiguration>>> GetFacilityAsync(
        CharacterId characterId,
        FacilityProductionType productionType,
        CancellationToken cancellationToken = default);
}

public sealed record ResolvedIndustryFacilityConfiguration(
    IndustryFacilityConfigurationRecord Configuration,
    Maybe<IndustryStructureRecord> Structure,
    IReadOnlyList<IndustryFacilityModuleRecord> InstalledModules);