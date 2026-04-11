using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Characters.Models;
using EVE.IPH.Domain.Core;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class AssetsScreenService : IAssetsScreenService
{
    private readonly IAssetReadRepository _assetReadRepository;
    private readonly ICharacterManagementService _characterManagementService;
    private readonly ICharacterRepository _characterRepository;

    public AssetsScreenService(
        IAssetReadRepository assetReadRepository,
        ICharacterManagementService characterManagementService,
        ICharacterRepository characterRepository)
    {
        _assetReadRepository = assetReadRepository ?? throw new ArgumentNullException(nameof(assetReadRepository));
        _characterManagementService = characterManagementService ?? throw new ArgumentNullException(nameof(characterManagementService));
        _characterRepository = characterRepository ?? throw new ArgumentNullException(nameof(characterRepository));
    }

    public async Task<AssetsScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<AssetScreenRecord>> assetsResult = await _assetReadRepository
            .GetHydratedAssetsAsync(cancellationToken)
            .ConfigureAwait(false);

        Result<IReadOnlyList<CharacterRecord>> charactersResult = await _characterRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);
        Result<IReadOnlyList<CorporationConnectionRecord>> corporationsResult = await _characterManagementService
            .GetCorporationsAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<HydratedAsset> assets = MapAssets(assetsResult);
        IReadOnlyList<AssetOwnerFilterOption> ownerOptions = BuildOwnerOptions(assets, charactersResult, corporationsResult);

        return new AssetsScreenData(assets, ownerOptions, BuildLoadStatusText(assetsResult, charactersResult, corporationsResult, assets));
    }

    public async Task<AssetsScreenData> RefreshAsync(CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<CharacterRecord>> charactersResult = await _characterRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        if (charactersResult.IsFailure)
        {
            AssetsScreenData fallback = await GetScreenDataAsync(cancellationToken).ConfigureAwait(false);
            return fallback with { StatusText = $"Unable to load stored characters for asset refresh: {charactersResult.Error.Message}" };
        }

        IReadOnlyList<CharacterRecord> refreshableCharacters = charactersResult.Value
            .Where(character => !SpecialCharacters.IsAllSkillsV(character.CharacterId))
            .ToArray();

        if (refreshableCharacters.Count == 0)
        {
            AssetsScreenData fallback = await GetScreenDataAsync(cancellationToken).ConfigureAwait(false);
            return fallback with { StatusText = "No connected characters are available for asset refresh yet. Connect and sync a character first." };
        }

        List<string> failures = [];
        int refreshedCount = 0;

        foreach (CharacterRecord character in refreshableCharacters)
        {
            Result<CharacterRecord> refreshResult = await _characterManagementService
                .RefreshAsync(character.CharacterId, cancellationToken)
                .ConfigureAwait(false);

            if (refreshResult.IsFailure)
            {
                failures.Add($"{character.Name}: {refreshResult.Error.Message}");
                continue;
            }

            refreshedCount++;
        }

        Result<IReadOnlyList<CorporationConnectionRecord>> corporationsResult = await _characterManagementService
            .GetCorporationsAsync(cancellationToken)
            .ConfigureAwait(false);

        if (corporationsResult.IsSuccess)
        {
            foreach (CorporationConnectionRecord corporation in corporationsResult.Value)
            {
                Result<CorporationConnectionRecord> refreshResult = await _characterManagementService
                    .RefreshCorporationAsync(corporation.CorporationId, cancellationToken)
                    .ConfigureAwait(false);

                if (refreshResult.IsFailure)
                {
                    failures.Add($"{corporation.Name}: {refreshResult.Error.Message}");
                    continue;
                }

                refreshedCount++;
            }
        }
        else
        {
            failures.Add($"Corporations: {corporationsResult.Error.Message}");
        }

        AssetsScreenData screenData = await GetScreenDataAsync(cancellationToken).ConfigureAwait(false);
        return screenData with { StatusText = BuildRefreshStatusText(refreshedCount, failures) };
    }

    private static IReadOnlyList<HydratedAsset> MapAssets(Result<IReadOnlyList<AssetScreenRecord>> result)
    {
        if (result.IsFailure)
        {
            return [];
        }

        return result.Value
            .Select(asset => new HydratedAsset(
                asset.OwnerId,
                asset.ItemId,
                asset.LocationId,
                new TypeId(asset.TypeId),
                asset.Quantity,
                asset.FlagId,
                asset.IsSingleton,
                asset.IsBlueprintCopy ? AssetBlueprintKind.Copy : AssetBlueprintKind.Original,
                asset.ItemName,
                asset.TypeName,
                asset.GroupName,
                asset.CategoryName,
                asset.LocationName,
                asset.FlagText,
                asset.Container,
                asset.SortOrder))
            .ToArray();
    }

    private static IReadOnlyList<AssetOwnerFilterOption> BuildOwnerOptions(
        IReadOnlyList<HydratedAsset> assets,
        Result<IReadOnlyList<CharacterRecord>> charactersResult,
        Result<IReadOnlyList<CorporationConnectionRecord>> corporationsResult)
    {
        Dictionary<long, string> characterNames = charactersResult.IsSuccess
            ? charactersResult.Value.ToDictionary(character => character.CharacterId.Value, character => character.Name)
            : [];
        Dictionary<long, string> corporationNames = corporationsResult.IsSuccess
            ? corporationsResult.Value.ToDictionary(corporation => corporation.CorporationId.Value, corporation => corporation.Name)
            : [];

        List<AssetOwnerFilterOption> ownerOptions =
        [
            new AssetOwnerFilterOption(null, "All Owners")
        ];

        ownerOptions.AddRange(
            assets.Select(asset => asset.OwnerId)
                .Distinct()
                .OrderBy(ownerId => ResolveOwnerName(ownerId, characterNames, corporationNames), StringComparer.OrdinalIgnoreCase)
                .Select(ownerId => new AssetOwnerFilterOption(
                    ownerId,
                    ResolveOwnerName(ownerId, characterNames, corporationNames))));

        return ownerOptions;
    }

    private static string BuildLoadStatusText(
        Result<IReadOnlyList<AssetScreenRecord>> assetsResult,
        Result<IReadOnlyList<CharacterRecord>> charactersResult,
        Result<IReadOnlyList<CorporationConnectionRecord>> corporationsResult,
        IReadOnlyList<HydratedAsset> assets)
    {
        if (assetsResult.IsFailure)
        {
            return $"Unable to load synced assets: {assetsResult.Error.Message}";
        }

        if (charactersResult.IsFailure)
        {
            return assets.Count == 0
                ? $"No synced asset records were found, and character names could not be loaded: {charactersResult.Error.Message}"
                : $"Loaded synced assets, but character names could not be loaded: {charactersResult.Error.Message}";
        }

        IReadOnlyList<CharacterRecord> realCharacters = charactersResult.Value
            .Where(character => !SpecialCharacters.IsAllSkillsV(character.CharacterId))
            .ToArray();
        int corporationCount = corporationsResult.IsSuccess ? corporationsResult.Value.Count : 0;

        if (assets.Count == 0)
        {
            return realCharacters.Count == 0 && corporationCount == 0
                ? "No connected characters or corporations are available yet. Connect and sync a character to load assets from ESI."
                : "No synced asset records were found yet. Use Refresh Assets to pull the latest character and corporation assets from ESI.";
        }

        return "Loaded synced asset records from the local SQLite store.";
    }

    private static string ResolveOwnerName(
        long ownerId,
        IReadOnlyDictionary<long, string> characterNames,
        IReadOnlyDictionary<long, string> corporationNames)
    {
        if (characterNames.TryGetValue(ownerId, out string? characterName))
        {
            return characterName;
        }

        if (corporationNames.TryGetValue(ownerId, out string? corporationName))
        {
            return corporationName;
        }

        return $"Owner {ownerId}";
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