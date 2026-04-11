using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Models;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class ManufacturingFacilityConfigurationService(
    IIndustryFacilityRepository industryFacilityRepository) : IManufacturingFacilityConfigurationService
{
    private readonly IIndustryFacilityRepository _industryFacilityRepository = industryFacilityRepository ?? throw new ArgumentNullException(nameof(industryFacilityRepository));

    public async Task<Result<IReadOnlyList<ResolvedIndustryFacilityConfiguration>>> GetFacilitiesAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<IndustryFacilityConfigurationRecord>> facilitiesResult = await _industryFacilityRepository
            .GetFacilitiesAsync(characterId, cancellationToken)
            .ConfigureAwait(false);

        if (facilitiesResult.IsFailure)
        {
            return Result<IReadOnlyList<ResolvedIndustryFacilityConfiguration>>.Failure(facilitiesResult.Error);
        }

        List<ResolvedIndustryFacilityConfiguration> resolved = [];
        foreach (IndustryFacilityConfigurationRecord configuration in facilitiesResult.Value)
        {
            Result<ResolvedIndustryFacilityConfiguration> resolveResult = await ResolveAsync(configuration, cancellationToken).ConfigureAwait(false);
            if (resolveResult.IsFailure)
            {
                return Result<IReadOnlyList<ResolvedIndustryFacilityConfiguration>>.Failure(resolveResult.Error);
            }

            resolved.Add(resolveResult.Value);
        }

        return Result<IReadOnlyList<ResolvedIndustryFacilityConfiguration>>.Success(resolved);
    }

    public async Task<Result<Maybe<ResolvedIndustryFacilityConfiguration>>> GetFacilityAsync(
        CharacterId characterId,
        FacilityProductionType productionType,
        CancellationToken cancellationToken = default)
    {
        Result<Maybe<IndustryFacilityConfigurationRecord>> facilityResult = await _industryFacilityRepository
            .GetFacilityAsync(characterId, productionType, cancellationToken)
            .ConfigureAwait(false);

        if (facilityResult.IsFailure)
        {
            return Result<Maybe<ResolvedIndustryFacilityConfiguration>>.Failure(facilityResult.Error);
        }

        if (facilityResult.Value.HasNoValue)
        {
            return Result<Maybe<ResolvedIndustryFacilityConfiguration>>.Success(Maybe<ResolvedIndustryFacilityConfiguration>.None);
        }

        Result<ResolvedIndustryFacilityConfiguration> resolveResult = await ResolveAsync(facilityResult.Value.Value, cancellationToken).ConfigureAwait(false);
        if (resolveResult.IsFailure)
        {
            return Result<Maybe<ResolvedIndustryFacilityConfiguration>>.Failure(resolveResult.Error);
        }

        return Result<Maybe<ResolvedIndustryFacilityConfiguration>>.Success(Maybe<ResolvedIndustryFacilityConfiguration>.Some(resolveResult.Value));
    }

    private async Task<Result<ResolvedIndustryFacilityConfiguration>> ResolveAsync(
        IndustryFacilityConfigurationRecord configuration,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<IndustryFacilityModuleRecord>> modulesResult = await _industryFacilityRepository
            .GetInstalledModulesAsync(configuration.CharacterId, configuration.ProductionType, configuration.FacilityId, cancellationToken)
            .ConfigureAwait(false);

        if (modulesResult.IsFailure)
        {
            return Result<ResolvedIndustryFacilityConfiguration>.Failure(modulesResult.Error);
        }

        Maybe<IndustryStructureRecord> structure = Maybe<IndustryStructureRecord>.None;
        if (configuration.FacilityKind == IndustryFacilityKind.UpwellStructure)
        {
            Result<Maybe<IndustryStructureRecord>> structureResult = await _industryFacilityRepository
                .GetStructureAsync(configuration.FacilityId, cancellationToken)
                .ConfigureAwait(false);

            if (structureResult.IsFailure)
            {
                return Result<ResolvedIndustryFacilityConfiguration>.Failure(structureResult.Error);
            }

            structure = structureResult.Value;
        }

        return Result<ResolvedIndustryFacilityConfiguration>.Success(new ResolvedIndustryFacilityConfiguration(configuration, structure, modulesResult.Value));
    }
}