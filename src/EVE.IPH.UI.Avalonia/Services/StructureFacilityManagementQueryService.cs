using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class StructureFacilityManagementQueryService(
    ICharacterManagementQueryService characterManagementQueryService,
    IIndustryFacilityRepository industryFacilityRepository,
    IManufacturingFacilityConfigurationService manufacturingFacilityConfigurationService) : IStructureFacilityManagementQueryService
{
    private readonly ICharacterManagementQueryService _characterManagementQueryService = characterManagementQueryService ?? throw new ArgumentNullException(nameof(characterManagementQueryService));
    private readonly IIndustryFacilityRepository _industryFacilityRepository = industryFacilityRepository ?? throw new ArgumentNullException(nameof(industryFacilityRepository));
    private readonly IManufacturingFacilityConfigurationService _manufacturingFacilityConfigurationService = manufacturingFacilityConfigurationService ?? throw new ArgumentNullException(nameof(manufacturingFacilityConfigurationService));

    public async Task<StructureFacilityManagementScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<EVE.IPH.Domain.Core.Models.IndustryStructureRecord>> structuresResult = await _industryFacilityRepository
            .GetStructuresAsync(cancellationToken)
            .ConfigureAwait(false);

        if (structuresResult.IsFailure)
        {
            return BuildEmpty($"Unable to load saved structures: {structuresResult.Error.Message}");
        }

        Result<CharacterManagementScreenData> charactersResult = await _characterManagementQueryService
            .GetScreenDataAsync(cancellationToken)
            .ConfigureAwait(false);

        if (charactersResult.IsFailure)
        {
            return new StructureFacilityManagementScreenData(
                [],
                MapStructures(structuresResult.Value),
                [],
                BuildProductionTypes(),
                BuildFacilityKinds(),
                $"Unable to load connected characters for facility settings: {charactersResult.Error.Message}");
        }

        IReadOnlyList<StructureFacilityCharacterOption> characters = charactersResult.Value.Characters
            .Where(row => !EVE.IPH.Domain.Core.SpecialCharacters.IsAllSkillsV(row.Character.CharacterId))
            .Select(row => new StructureFacilityCharacterOption(row.Character.CharacterId, row.Character.Name, row.Character.IsDefault))
            .OrderByDescending(row => row.IsDefault)
            .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        List<FacilitySettingsRow> facilities = [];
        foreach (StructureFacilityCharacterOption character in characters)
        {
            Result<IReadOnlyList<ResolvedIndustryFacilityConfiguration>> facilityResult = await _manufacturingFacilityConfigurationService
                .GetFacilitiesAsync(character.CharacterId, cancellationToken)
                .ConfigureAwait(false);

            if (facilityResult.IsFailure)
            {
                return new StructureFacilityManagementScreenData(
                    characters,
                    MapStructures(structuresResult.Value),
                    [],
                    BuildProductionTypes(),
                    BuildFacilityKinds(),
                    $"Unable to load facility settings for {character.Name}: {facilityResult.Error.Message}");
            }

            facilities.AddRange(facilityResult.Value.Select(configuration => new FacilitySettingsRow(
                configuration.Configuration.CharacterId,
                character.Name,
                configuration.Configuration.ProductionType,
                configuration.Configuration.FacilityId,
                configuration.Configuration.FacilityName,
                configuration.Configuration.FacilityKind,
                configuration.Structure.HasValue ? configuration.Structure.Value.StructureId : null,
                configuration.Structure.HasValue ? configuration.Structure.Value.StructureName : null,
                configuration.Configuration.RegionId,
                configuration.Configuration.RegionName,
                configuration.Configuration.SolarSystemId,
                configuration.Configuration.SolarSystemName,
                configuration.Configuration.SolarSystemSecurity,
                configuration.Configuration.CostIndex,
                configuration.Configuration.ActivityCostPerSecond,
                configuration.Configuration.IncludeActivityCost,
                configuration.Configuration.IncludeActivityTime,
                configuration.Configuration.IncludeActivityUsage,
                configuration.Configuration.ConvertToOre,
                configuration.Configuration.FactionWarfareUpgradeLevel,
                configuration.Configuration.TaxRate,
                configuration.Configuration.MaterialMultiplierOverride.HasValue ? configuration.Configuration.MaterialMultiplierOverride.Value : null,
                configuration.Configuration.TimeMultiplierOverride.HasValue ? configuration.Configuration.TimeMultiplierOverride.Value : null,
                configuration.Configuration.CostMultiplierOverride.HasValue ? configuration.Configuration.CostMultiplierOverride.Value : null,
                configuration.InstalledModules.Select(module => module.ModuleTypeId).OrderBy(id => id).ToArray())));
        }

        string statusText = characters.Count == 0
            ? "No connected characters are available for facility settings yet. Connect a character to start saving facility selections."
            : $"Loaded {structuresResult.Value.Count} saved structures and {facilities.Count} facility settings across {characters.Count} connected characters.";

        return new StructureFacilityManagementScreenData(
            characters,
            MapStructures(structuresResult.Value),
            facilities.OrderBy(row => row.CharacterName, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.ProductionTypeDisplayName, StringComparer.OrdinalIgnoreCase).ToArray(),
            BuildProductionTypes(),
            BuildFacilityKinds(),
            statusText);
    }

    private static IReadOnlyList<IndustryStructureRow> MapStructures(IReadOnlyList<EVE.IPH.Domain.Core.Models.IndustryStructureRecord> structures) => structures
        .Select(structure => new IndustryStructureRow(
            structure.StructureId,
            structure.StructureName,
            structure.StructureTypeId,
            structure.SolarSystemId,
            structure.RegionId,
            structure.OwnerCorporationId.HasValue ? structure.OwnerCorporationId.Value : null,
            structure.IsManualEntry,
            structure.UpdatedAtUtc))
        .OrderBy(row => row.StructureName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static IReadOnlyList<FacilityProductionTypeOption> BuildProductionTypes() => Enum
        .GetValues<FacilityProductionType>()
        .Where(value => value != FacilityProductionType.None)
        .Select(value => new FacilityProductionTypeOption(value, value switch
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
            _ => value.ToString(),
        }))
        .ToArray();

    private static IReadOnlyList<FacilityKindOption> BuildFacilityKinds() => Enum
        .GetValues<IndustryFacilityKind>()
        .Select(value => new FacilityKindOption(value, value switch
        {
            IndustryFacilityKind.UpwellStructure => "Upwell Structure",
            _ => value.ToString(),
        }))
        .ToArray();

    private static StructureFacilityManagementScreenData BuildEmpty(string statusText) =>
        new([], [], [], BuildProductionTypes(), BuildFacilityKinds(), statusText);
}