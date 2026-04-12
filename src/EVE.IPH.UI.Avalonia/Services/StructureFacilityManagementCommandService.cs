using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Models;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class StructureFacilityManagementCommandService(
    IIndustryFacilityRepository industryFacilityRepository,
    ICharacterManagementQueryService characterManagementQueryService) : IStructureFacilityManagementCommandService
{
    private readonly IIndustryFacilityRepository _industryFacilityRepository = industryFacilityRepository ?? throw new ArgumentNullException(nameof(industryFacilityRepository));
    private readonly ICharacterManagementQueryService _characterManagementQueryService = characterManagementQueryService ?? throw new ArgumentNullException(nameof(characterManagementQueryService));

    public Task<Result<IndustryStructureRecord>> SaveStructureAsync(StructureUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (request.StructureId <= 0)
        {
            return Task.FromResult(Result<IndustryStructureRecord>.Failure("INVALID_STRUCTURE_ID", "Structure ID must be greater than zero."));
        }

        IndustryStructureRecord structure = new(
            request.StructureId,
            request.StructureName.Trim(),
            request.StructureTypeId,
            request.SolarSystemId,
            request.RegionId,
            request.OwnerCorporationId.HasValue ? Maybe<long>.Some(request.OwnerCorporationId.Value) : Maybe<long>.None,
            request.IsManualEntry,
            DateTimeOffset.UtcNow);

        return _industryFacilityRepository.UpsertStructureAsync(structure, cancellationToken);
    }

    public async Task<Result<bool>> DeleteStructureAsync(long structureId, CancellationToken cancellationToken = default)
    {
        Result<CharacterManagementScreenData> characters = await _characterManagementQueryService
            .GetScreenDataAsync(cancellationToken)
            .ConfigureAwait(false);

        if (characters.IsSuccess)
        {
            foreach (CharacterManagementCharacterRow character in characters.Value.Characters.Where(row => !EVE.IPH.Domain.Core.SpecialCharacters.IsAllSkillsV(row.Character.CharacterId)))
            {
                Result<IReadOnlyList<IndustryFacilityConfigurationRecord>> facilities = await _industryFacilityRepository
                    .GetFacilitiesAsync(character.Character.CharacterId, cancellationToken)
                    .ConfigureAwait(false);

                if (facilities.IsFailure)
                {
                    return Result<bool>.Failure(facilities.Error);
                }

                foreach (IndustryFacilityConfigurationRecord facility in facilities.Value.Where(facility => facility.FacilityKind == IndustryFacilityKind.UpwellStructure && facility.FacilityId == structureId))
                {
                    Result<IReadOnlyList<IndustryFacilityModuleRecord>> moduleDelete = await _industryFacilityRepository
                        .ReplaceInstalledModulesAsync(facility.CharacterId, facility.ProductionType, facility.FacilityId, [], cancellationToken)
                        .ConfigureAwait(false);

                    if (moduleDelete.IsFailure)
                    {
                        return Result<bool>.Failure(moduleDelete.Error);
                    }

                    Result<bool> facilityDelete = await _industryFacilityRepository
                        .DeleteFacilityAsync(facility.CharacterId, facility.ProductionType, cancellationToken)
                        .ConfigureAwait(false);

                    if (facilityDelete.IsFailure)
                    {
                        return Result<bool>.Failure(facilityDelete.Error);
                    }
                }
            }
        }

        return await _industryFacilityRepository.DeleteStructureAsync(structureId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<IndustryFacilityConfigurationRecord>> SaveFacilityAsync(FacilitySettingsUpsertRequest request, CancellationToken cancellationToken = default)
    {
        IndustryFacilityConfigurationRecord configuration = new(
            request.CharacterId,
            request.ProductionType,
            request.FacilityId,
            request.FacilityName.Trim(),
            request.FacilityKind,
            request.RegionId,
            request.RegionName.Trim(),
            request.SolarSystemId,
            request.SolarSystemName.Trim(),
            request.SolarSystemSecurity,
            request.CostIndex,
            request.ActivityCostPerSecond,
            request.IncludeActivityCost,
            request.IncludeActivityTime,
            request.IncludeActivityUsage,
            request.ConvertToOre,
            request.FactionWarfareUpgradeLevel,
            request.TaxRate,
            request.MaterialMultiplierOverride.HasValue ? Maybe<double>.Some(request.MaterialMultiplierOverride.Value) : Maybe<double>.None,
            request.TimeMultiplierOverride.HasValue ? Maybe<double>.Some(request.TimeMultiplierOverride.Value) : Maybe<double>.None,
            request.CostMultiplierOverride.HasValue ? Maybe<double>.Some(request.CostMultiplierOverride.Value) : Maybe<double>.None);

        Result<IndustryFacilityConfigurationRecord> upsertResult = await _industryFacilityRepository
            .UpsertFacilityAsync(configuration, cancellationToken)
            .ConfigureAwait(false);

        if (upsertResult.IsFailure)
        {
            return upsertResult;
        }

        Result<IReadOnlyList<IndustryFacilityModuleRecord>> moduleResult = await _industryFacilityRepository
            .ReplaceInstalledModulesAsync(request.CharacterId, request.ProductionType, request.FacilityId, request.ModuleTypeIds, cancellationToken)
            .ConfigureAwait(false);

        if (moduleResult.IsFailure)
        {
            return Result<IndustryFacilityConfigurationRecord>.Failure(moduleResult.Error);
        }

        return upsertResult;
    }

    public async Task<Result<bool>> DeleteFacilityAsync(CharacterId characterId, FacilityProductionType productionType, CancellationToken cancellationToken = default)
    {
        Result<Maybe<IndustryFacilityConfigurationRecord>> existing = await _industryFacilityRepository
            .GetFacilityAsync(characterId, productionType, cancellationToken)
            .ConfigureAwait(false);

        if (existing.IsFailure)
        {
            return Result<bool>.Failure(existing.Error);
        }

        if (existing.Value.HasNoValue)
        {
            return Result<bool>.Success(false);
        }

        Result<IReadOnlyList<IndustryFacilityModuleRecord>> moduleDelete = await _industryFacilityRepository
            .ReplaceInstalledModulesAsync(characterId, productionType, existing.Value.Value.FacilityId, [], cancellationToken)
            .ConfigureAwait(false);

        if (moduleDelete.IsFailure)
        {
            return Result<bool>.Failure(moduleDelete.Error);
        }

        return await _industryFacilityRepository.DeleteFacilityAsync(characterId, productionType, cancellationToken).ConfigureAwait(false);
    }
}