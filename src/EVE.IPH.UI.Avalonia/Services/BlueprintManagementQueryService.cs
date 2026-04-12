using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class BlueprintManagementQueryService(
    ICharacterManagementQueryService characterManagementQueryService,
    IOwnedBlueprintWorkflowService ownedBlueprintWorkflowService) : IBlueprintManagementQueryService
{
    private readonly ICharacterManagementQueryService _characterManagementQueryService = characterManagementQueryService ?? throw new ArgumentNullException(nameof(characterManagementQueryService));
    private readonly IOwnedBlueprintWorkflowService _ownedBlueprintWorkflowService = ownedBlueprintWorkflowService ?? throw new ArgumentNullException(nameof(ownedBlueprintWorkflowService));

    public async Task<BlueprintManagementScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default)
    {
        var managementResult = await _characterManagementQueryService
            .GetScreenDataAsync(cancellationToken)
            .ConfigureAwait(false);

        if (managementResult.IsFailure)
        {
            return new BlueprintManagementScreenData([], [new BlueprintOwnerFilterOption(null, "All Owners")], $"Unable to load blueprint owners: {managementResult.Error.Message}");
        }

        IReadOnlyList<BlueprintOwnerFilterOption> ownerOptions = BuildOwnerOptions(managementResult.Value);
        long[] ownerIds = ownerOptions
            .Where(option => option.OwnerId.HasValue)
            .Select(option => option.OwnerId!.Value)
            .ToArray();

        var blueprintsResult = await _ownedBlueprintWorkflowService
            .GetBlueprintsByOwnersAsync(ownerIds, cancellationToken)
            .ConfigureAwait(false);

        if (blueprintsResult.IsFailure)
        {
            return new BlueprintManagementScreenData([], ownerOptions, $"Unable to load owned blueprints: {blueprintsResult.Error.Message}");
        }

        IReadOnlyList<BlueprintManagementRow> rows = blueprintsResult.Value
            .Select(record => new BlueprintManagementRow(
                record.OwnerId,
                record.OwnerName,
                record.IsCorporationOwner,
                new EVE.IPH.Domain.Core.Identifiers.ItemId(record.ItemId),
                record.LocationId,
                new BlueprintId(record.BlueprintId),
                record.BlueprintName,
                record.Quantity,
                record.Me,
                record.Te,
                record.Runs,
                record.BpType,
                record.Owned,
                record.Scanned))
            .OrderBy(row => row.OwnerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.BlueprintName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new BlueprintManagementScreenData(rows, ownerOptions, BuildStatusText(managementResult.Value, rows.Count));
    }

    private static IReadOnlyList<BlueprintOwnerFilterOption> BuildOwnerOptions(CharacterManagementScreenData screenData)
    {
        List<BlueprintOwnerFilterOption> options = [new BlueprintOwnerFilterOption(null, "All Owners")];

        options.AddRange(screenData.Characters
            .Where(row => !EVE.IPH.Domain.Core.SpecialCharacters.IsAllSkillsV(row.Character.CharacterId))
            .Select(row => new BlueprintOwnerFilterOption(row.Character.CharacterId.Value, row.Character.Name))
            .OrderBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase));

        options.AddRange(screenData.Corporations
            .Select(row => new BlueprintOwnerFilterOption(row.Corporation.CorporationId.Value, row.Corporation.Name))
            .OrderBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase));

        return options;
    }

    private static string BuildStatusText(CharacterManagementScreenData screenData, int blueprintCount)
    {
        int ownerCount = screenData.Characters.Count(character => !EVE.IPH.Domain.Core.SpecialCharacters.IsAllSkillsV(character.Character.CharacterId))
            + screenData.Corporations.Count;

        if (ownerCount == 0)
        {
            return "No connected characters or corporations are available yet. Connect and sync a character to manage owned blueprints.";
        }

        if (blueprintCount == 0)
        {
            return "No owned blueprints were found for the connected characters and corporations yet.";
        }

        return "Loaded owned blueprints for the connected characters and corporations.";
    }
}