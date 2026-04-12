using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Services;
using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Core;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class BlueprintManagementCommandService(
    IBlueprintManagementQueryService blueprintManagementQueryService,
    ICharacterManagementQueryService characterManagementQueryService,
    ICharacterManagementCommandService characterManagementCommandService,
    IOwnedBlueprintWorkflowService ownedBlueprintWorkflowService) : IBlueprintManagementCommandService
{
    private readonly IBlueprintManagementQueryService _blueprintManagementQueryService = blueprintManagementQueryService ?? throw new ArgumentNullException(nameof(blueprintManagementQueryService));
    private readonly ICharacterManagementQueryService _characterManagementQueryService = characterManagementQueryService ?? throw new ArgumentNullException(nameof(characterManagementQueryService));
    private readonly ICharacterManagementCommandService _characterManagementCommandService = characterManagementCommandService ?? throw new ArgumentNullException(nameof(characterManagementCommandService));
    private readonly IOwnedBlueprintWorkflowService _ownedBlueprintWorkflowService = ownedBlueprintWorkflowService ?? throw new ArgumentNullException(nameof(ownedBlueprintWorkflowService));

    public async Task<BlueprintManagementScreenData> RefreshAsync(CancellationToken cancellationToken = default)
    {
        Result<CharacterManagementScreenData> managementResult = await _characterManagementQueryService
            .GetScreenDataAsync(cancellationToken)
            .ConfigureAwait(false);

        if (managementResult.IsFailure)
        {
            BlueprintManagementScreenData fallback = await _blueprintManagementQueryService.GetScreenDataAsync(cancellationToken).ConfigureAwait(false);
            return fallback with { StatusText = $"Unable to load connected owners for blueprint refresh: {managementResult.Error.Message}" };
        }

        IReadOnlyList<CharacterManagementCharacterRow> refreshableCharacters = managementResult.Value.Characters
            .Where(character => !SpecialCharacters.IsAllSkillsV(character.Character.CharacterId))
            .ToArray();

        if (refreshableCharacters.Count == 0 && managementResult.Value.Corporations.Count == 0)
        {
            BlueprintManagementScreenData fallback = await _blueprintManagementQueryService.GetScreenDataAsync(cancellationToken).ConfigureAwait(false);
            return fallback with { StatusText = "No connected characters or corporations are available for blueprint refresh yet. Connect and sync a character first." };
        }

        List<string> failures = [];
        int refreshedCount = 0;

        foreach (CharacterManagementCharacterRow character in refreshableCharacters)
        {
            Result<CharacterRecord> refreshResult = await _characterManagementCommandService
                .RefreshAsync(character.Character.CharacterId, cancellationToken)
                .ConfigureAwait(false);

            if (refreshResult.IsFailure)
            {
                failures.Add($"{character.Character.Name}: {refreshResult.Error.Message}");
                continue;
            }

            refreshedCount++;
        }

        foreach (CharacterManagementCorporationRow corporation in managementResult.Value.Corporations)
        {
            Result<CorporationConnectionRecord> refreshResult = await _characterManagementCommandService
                .RefreshCorporationAsync(corporation.Corporation.CorporationId, cancellationToken)
                .ConfigureAwait(false);

            if (refreshResult.IsFailure)
            {
                failures.Add($"{corporation.Corporation.Name}: {refreshResult.Error.Message}");
                continue;
            }

            refreshedCount++;
        }

        BlueprintManagementScreenData screenData = await _blueprintManagementQueryService.GetScreenDataAsync(cancellationToken).ConfigureAwait(false);
        return screenData with { StatusText = BuildRefreshStatusText(refreshedCount, failures) };
    }

    public Task<Result<OwnedBlueprintRecord>> SaveBlueprintAsync(OwnedBlueprintRecord blueprint, CancellationToken cancellationToken = default) =>
        _ownedBlueprintWorkflowService.SaveBlueprintAsync(blueprint, cancellationToken);

    public Task<Result<bool>> DeleteBlueprintAsync(long ownerId, BlueprintId blueprintId, CancellationToken cancellationToken = default) =>
        _ownedBlueprintWorkflowService.DeleteBlueprintAsync(ownerId, blueprintId, cancellationToken);

    private static string BuildRefreshStatusText(int refreshedCount, IReadOnlyList<string> failures)
    {
        if (refreshedCount == 0 && failures.Count > 0)
        {
            return $"Unable to refresh blueprint owners from ESI. {string.Join(" | ", failures)}";
        }

        if (failures.Count == 0)
        {
            return $"Refreshed connected owner data for {refreshedCount} character{(refreshedCount == 1 ? string.Empty : "s")} and reloaded owned blueprints.";
        }

        return $"Refreshed connected owner data for {refreshedCount} character{(refreshedCount == 1 ? string.Empty : "s")} and reloaded owned blueprints. Failed: {string.Join(" | ", failures)}";
    }
}