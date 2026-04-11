using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Models;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

public interface IIndustryFacilityRepository
{
    Task<Result<IReadOnlyList<IndustryStructureRecord>>> GetStructuresAsync(CancellationToken cancellationToken = default);

    Task<Result<Maybe<IndustryStructureRecord>>> GetStructureAsync(long structureId, CancellationToken cancellationToken = default);

    Task<Result<IndustryStructureRecord>> UpsertStructureAsync(IndustryStructureRecord structure, CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteStructureAsync(long structureId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<IndustryFacilityConfigurationRecord>>> GetFacilitiesAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    Task<Result<Maybe<IndustryFacilityConfigurationRecord>>> GetFacilityAsync(
        CharacterId characterId,
        FacilityProductionType productionType,
        CancellationToken cancellationToken = default);

    Task<Result<IndustryFacilityConfigurationRecord>> UpsertFacilityAsync(
        IndustryFacilityConfigurationRecord configuration,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteFacilityAsync(
        CharacterId characterId,
        FacilityProductionType productionType,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<IndustryFacilityModuleRecord>>> GetInstalledModulesAsync(
        CharacterId characterId,
        FacilityProductionType productionType,
        long facilityId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<IndustryFacilityModuleRecord>>> ReplaceInstalledModulesAsync(
        CharacterId characterId,
        FacilityProductionType productionType,
        long facilityId,
        IReadOnlyList<int> moduleTypeIds,
        CancellationToken cancellationToken = default);
}