using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class ManufacturingWorkspaceQueryService(
    ICharacterManagementQueryService characterManagementQueryService,
    IOwnedBlueprintWorkflowService ownedBlueprintWorkflowService,
    IManufacturingFacilityConfigurationService manufacturingFacilityConfigurationService) : IManufacturingWorkspaceQueryService
{
    private readonly ICharacterManagementQueryService _characterManagementQueryService = characterManagementQueryService ?? throw new ArgumentNullException(nameof(characterManagementQueryService));
    private readonly IOwnedBlueprintWorkflowService _ownedBlueprintWorkflowService = ownedBlueprintWorkflowService ?? throw new ArgumentNullException(nameof(ownedBlueprintWorkflowService));
    private readonly IManufacturingFacilityConfigurationService _manufacturingFacilityConfigurationService = manufacturingFacilityConfigurationService ?? throw new ArgumentNullException(nameof(manufacturingFacilityConfigurationService));

    public async Task<ManufacturingWorkspaceScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default)
    {
        var managementResult = await _characterManagementQueryService
            .GetScreenDataAsync(cancellationToken)
            .ConfigureAwait(false);

        if (managementResult.IsFailure)
        {
            return new ManufacturingWorkspaceScreenData([], [], $"Unable to load manufacturing owners: {managementResult.Error.Message}");
        }

        long[] ownerIds = managementResult.Value.Characters
            .Where(row => !Domain.Core.SpecialCharacters.IsAllSkillsV(row.Character.CharacterId))
            .Select(row => row.Character.CharacterId.Value)
            .Concat(managementResult.Value.Corporations.Select(row => row.Corporation.CorporationId.Value))
            .Distinct()
            .ToArray();

        IReadOnlyList<ManufacturingBlueprintOption> blueprints = [];
        if (ownerIds.Length > 0)
        {
            var blueprintsResult = await _ownedBlueprintWorkflowService
                .GetBlueprintsByOwnersAsync(ownerIds, cancellationToken)
                .ConfigureAwait(false);

            if (blueprintsResult.IsFailure)
            {
                return new ManufacturingWorkspaceScreenData([], [], $"Unable to load manufacturing blueprints: {blueprintsResult.Error.Message}");
            }

            blueprints = blueprintsResult.Value
                .Select(record => new ManufacturingBlueprintOption(
                    record.OwnerId,
                    record.OwnerName,
                    record.IsCorporationOwner,
                    new BlueprintId(record.BlueprintId),
                    record.BlueprintName,
                    record.Me,
                    record.Te,
                    record.Runs,
                    record.Quantity,
                    record.Owned))
                .OrderBy(record => record.BlueprintName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(record => record.OwnerName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        List<ManufacturingFacilityOption> facilities = [];
        foreach (CharacterManagementCharacterRow characterRow in managementResult.Value.Characters.Where(row => !Domain.Core.SpecialCharacters.IsAllSkillsV(row.Character.CharacterId)))
        {
            var facilityResult = await _manufacturingFacilityConfigurationService
                .GetFacilityAsync(characterRow.Character.CharacterId, FacilityProductionType.Manufacturing, cancellationToken)
                .ConfigureAwait(false);

            if (facilityResult.IsFailure)
            {
                return new ManufacturingWorkspaceScreenData(blueprints, [], $"Unable to load manufacturing facilities: {facilityResult.Error.Message}");
            }

            if (facilityResult.Value.HasNoValue)
            {
                continue;
            }

            ResolvedIndustryFacilityConfiguration facility = facilityResult.Value.Value;
            facilities.Add(new ManufacturingFacilityOption(
                facility.Configuration.CharacterId,
                facility.Configuration.ProductionType,
                facility.Configuration.FacilityId,
                facility.Configuration.FacilityName,
                characterRow.Character.Name,
                facility.Configuration.RegionName,
                facility.Configuration.SolarSystemName,
                facility.Configuration.CostIndex,
                facility.Configuration.TaxRate));
        }

        facilities = facilities
            .OrderBy(option => option.CharacterName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(option => option.FacilityName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (blueprints.Count == 0 && facilities.Count == 0)
        {
            return new ManufacturingWorkspaceScreenData([], [], "Connect a character, sync owned blueprints, and save a manufacturing facility to start the manufacturing workspace.");
        }

        if (blueprints.Count == 0)
        {
            return new ManufacturingWorkspaceScreenData([], facilities, "No owned blueprints are available yet. Refresh blueprint data before running manufacturing analysis.");
        }

        if (facilities.Count == 0)
        {
            return new ManufacturingWorkspaceScreenData(blueprints, [], "No saved manufacturing facility is available yet. Configure a manufacturing facility before running analysis.");
        }

        return new ManufacturingWorkspaceScreenData(
            blueprints,
            facilities,
            $"Loaded {blueprints.Count} owned blueprints and {facilities.Count} saved manufacturing facilities for the first manufacturing workflow.");
    }
}