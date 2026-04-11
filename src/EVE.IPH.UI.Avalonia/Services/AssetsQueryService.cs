using EVE.IPH.Domain.Assets.Models;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class AssetsQueryService(
    IAssetReadRepository assetReadRepository,
    ICharacterManagementQueryService characterManagementQueryService) : IAssetsQueryService
{
    private readonly IAssetReadRepository _assetReadRepository = assetReadRepository ?? throw new ArgumentNullException(nameof(assetReadRepository));
    private readonly ICharacterManagementQueryService _characterManagementQueryService = characterManagementQueryService ?? throw new ArgumentNullException(nameof(characterManagementQueryService));

    public async Task<AssetsScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<AssetScreenRecord>> assetsResult = await _assetReadRepository
            .GetHydratedAssetsAsync(cancellationToken)
            .ConfigureAwait(false);

        Result<CharacterManagementScreenData> managementResult = await _characterManagementQueryService
            .GetScreenDataAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<HydratedAsset> assets = MapAssets(assetsResult);
        IReadOnlyList<AssetOwnerFilterOption> ownerOptions = BuildOwnerOptions(assets, managementResult);

        return new AssetsScreenData(assets, ownerOptions, BuildLoadStatusText(assetsResult, managementResult, assets));
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
        Result<CharacterManagementScreenData> managementResult)
    {
        Dictionary<long, string> characterNames = managementResult.IsSuccess
            ? managementResult.Value.Characters.ToDictionary(character => character.Character.CharacterId.Value, character => character.Character.Name)
            : [];
        Dictionary<long, string> corporationNames = managementResult.IsSuccess
            ? managementResult.Value.Corporations.ToDictionary(corporation => corporation.Corporation.CorporationId.Value, corporation => corporation.Corporation.Name)
            : [];

        List<AssetOwnerFilterOption> ownerOptions = [new AssetOwnerFilterOption(null, "All Owners")];

        ownerOptions.AddRange(
            assets.Select(asset => asset.OwnerId)
                .Distinct()
                .OrderBy(ownerId => ResolveOwnerName(ownerId, characterNames, corporationNames), StringComparer.OrdinalIgnoreCase)
                .Select(ownerId => new AssetOwnerFilterOption(ownerId, ResolveOwnerName(ownerId, characterNames, corporationNames))));

        return ownerOptions;
    }

    private static string BuildLoadStatusText(
        Result<IReadOnlyList<AssetScreenRecord>> assetsResult,
        Result<CharacterManagementScreenData> managementResult,
        IReadOnlyList<HydratedAsset> assets)
    {
        if (assetsResult.IsFailure)
        {
            return $"Unable to load synced assets: {assetsResult.Error.Message}";
        }

        if (managementResult.IsFailure)
        {
            return assets.Count == 0
                ? $"No synced asset records were found, and owner metadata could not be loaded: {managementResult.Error.Message}"
                : $"Loaded synced assets, but owner metadata could not be loaded: {managementResult.Error.Message}";
        }

        IReadOnlyList<CharacterManagementCharacterRow> realCharacters = managementResult.Value.Characters
            .Where(character => !EVE.IPH.Domain.Core.SpecialCharacters.IsAllSkillsV(character.Character.CharacterId))
            .ToArray();
        int corporationCount = managementResult.Value.Corporations.Count;

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
}