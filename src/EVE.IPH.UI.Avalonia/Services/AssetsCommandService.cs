using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Core;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class AssetsCommandService(
    IAssetsQueryService assetsQueryService,
    ICharacterManagementQueryService characterManagementQueryService,
    ICharacterManagementCommandService characterManagementCommandService) : IAssetsCommandService
{
    private readonly IAssetsQueryService _assetsQueryService = assetsQueryService ?? throw new ArgumentNullException(nameof(assetsQueryService));
    private readonly ICharacterManagementQueryService _characterManagementQueryService = characterManagementQueryService ?? throw new ArgumentNullException(nameof(characterManagementQueryService));
    private readonly ICharacterManagementCommandService _characterManagementCommandService = characterManagementCommandService ?? throw new ArgumentNullException(nameof(characterManagementCommandService));

    public async Task<AssetsScreenData> RefreshAsync(CancellationToken cancellationToken = default)
    {
        Result<CharacterManagementScreenData> managementResult = await _characterManagementQueryService
            .GetScreenDataAsync(cancellationToken)
            .ConfigureAwait(false);

        if (managementResult.IsFailure)
        {
            AssetsScreenData fallback = await _assetsQueryService.GetScreenDataAsync(cancellationToken).ConfigureAwait(false);
            return fallback with { StatusText = $"Unable to load stored characters for asset refresh: {managementResult.Error.Message}" };
        }

        IReadOnlyList<CharacterManagementCharacterRow> refreshableCharacters = managementResult.Value.Characters
            .Where(character => !SpecialCharacters.IsAllSkillsV(character.Character.CharacterId))
            .ToArray();

        if (refreshableCharacters.Count == 0)
        {
            AssetsScreenData fallback = await _assetsQueryService.GetScreenDataAsync(cancellationToken).ConfigureAwait(false);
            return fallback with { StatusText = "No connected characters are available for asset refresh yet. Connect and sync a character first." };
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

        AssetsScreenData screenData = await _assetsQueryService.GetScreenDataAsync(cancellationToken).ConfigureAwait(false);
        return screenData with { StatusText = BuildRefreshStatusText(refreshedCount, failures) };
    }

    private static string BuildRefreshStatusText(int refreshedCount, IReadOnlyList<string> failures)
    {
        if (refreshedCount == 0 && failures.Count > 0)
        {
            return $"Unable to refresh assets from ESI. {string.Join(" | ", failures)}";
        }

        if (failures.Count == 0)
        {
            return $"Refreshed assets for {refreshedCount} connected character{(refreshedCount == 1 ? string.Empty : "s")}.";
        }

        return $"Refreshed assets for {refreshedCount} connected character{(refreshedCount == 1 ? string.Empty : "s")}. Failed: {string.Join(" | ", failures)}";
    }
}