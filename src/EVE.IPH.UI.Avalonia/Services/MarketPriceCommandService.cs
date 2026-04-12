using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Market.Services;
using EVE.IPH.Infrastructure.Settings.Models;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class MarketPriceCommandService(
    IMarketService marketService,
    IItemRepository itemRepository,
    ISettingsStore settingsStore) : IMarketPriceCommandService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);
    private static readonly string[] GasGroupNames = ["Harvestable Cloud", "Compressed Gas"];
    private static readonly string[] RawMaterialGroupNames = ["Abyssal Materials"];
    private static readonly string[] SalvageGroupNames = ["Salvaged Materials", "Ancient Salvage"];
    private static readonly string[] AdvancedComponentGroupNames = ["Construction Components"];
    private static readonly string[] FuelBlockGroupNames = ["Fuel Block"];

    private readonly IMarketService _marketService = marketService ?? throw new ArgumentNullException(nameof(marketService));
    private readonly IItemRepository _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
    private readonly ISettingsStore _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));

    public async Task<Result<MarketPriceResult>> LoadPricesAsync(MarketPriceRequest request, CancellationToken cancellationToken = default)
    {
        if (request.RegionId <= 0)
        {
            return Result<MarketPriceResult>.Failure("INVALID_REGION_ID", "Region ID must be greater than zero.");
        }

        Result<IReadOnlyList<TypeId>> typeIdsResult = ParseTypeIds(request.TypeIdsText);
        if (typeIdsResult.IsFailure)
        {
            return Result<MarketPriceResult>.Failure(typeIdsResult.Error);
        }

        Result<IReadOnlyDictionary<TypeId, MarketPrice>> pricesResult = await _marketService
            .GetPricesAsync(typeIdsResult.Value, new RegionId((int)request.RegionId), request.SourceKind, CacheDuration, cancellationToken)
            .ConfigureAwait(false);

        if (pricesResult.IsFailure)
        {
            return Result<MarketPriceResult>.Failure(pricesResult.Error);
        }

        List<MarketPriceRow> rows = [];
        foreach (TypeId typeId in typeIdsResult.Value)
        {
            pricesResult.Value.TryGetValue(typeId, out MarketPrice? price);
            Maybe<string> itemName = await _itemRepository.GetItemNameAsync(typeId, cancellationToken).ConfigureAwait(false);

            rows.Add(new MarketPriceRow(
                typeId.Value,
                itemName.HasValue ? itemName.Value : string.Empty,
                price?.MinSell,
                price?.MaxBuy,
                price?.Average));
        }

        IReadOnlyList<MarketPriceRow> sortedRows = rows
            .OrderBy(row => row.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Result<bool> persistResult = await PersistWorkspaceSettingsAsync(
            request.RegionId,
            string.Join(", ", typeIdsResult.Value.Select(typeId => typeId.Value)),
            request.SourceKind,
            cancellationToken).ConfigureAwait(false);

        string statusText = $"Loaded {sortedRows.Count} market price snapshot{(sortedRows.Count == 1 ? string.Empty : "s")} for region {request.RegionId} using {request.SourceKind}.";
        if (persistResult.IsFailure)
        {
            statusText = $"{statusText} Workspace settings could not be saved: {persistResult.Error.Message}";
        }

        return Result<MarketPriceResult>.Success(new MarketPriceResult(
            sortedRows,
            statusText));
    }

    public async Task<Result<MarketPriceWatchlistResult>> BuildWatchlistFromSavedSelectionAsync(CancellationToken cancellationToken = default)
    {
        Maybe<UpdatePriceTabSettingsModel> settingsResult = await ReadUpdatePriceSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (!settingsResult.HasValue)
        {
            return Result<MarketPriceWatchlistResult>.Failure(
                "MISSING_UPDATE_PRICE_SETTINGS",
                "No saved update-price categories were found yet. Save category settings first, then rebuild the watchlist here.");
        }

        UpdatePriceTabSettingsModel settings = settingsResult.Value;
        bool includeAllRawMaterials = settings.AllRawMats;
        List<string> includedSections = [];
        List<ItemRecord> items = [];

        Result<bool> appendResult = await AppendSelectedItemsAsync(items, settings, includeAllRawMaterials, includedSections, cancellationToken).ConfigureAwait(false);
        if (appendResult.IsFailure)
        {
            return Result<MarketPriceWatchlistResult>.Failure(appendResult.Error);
        }

        IReadOnlyList<long> typeIds = items
            .OrderBy(item => item.TypeName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.TypeId.Value)
            .Select(item => item.TypeId.Value)
            .Distinct()
            .ToArray();

        if (typeIds.Count == 0)
        {
            return Result<MarketPriceWatchlistResult>.Failure(
                "NO_SUPPORTED_CATEGORY_SELECTIONS",
                "The saved update-price categories do not include any of the modern Market tab's supported category imports yet.");
        }

        string typeIdsText = string.Join(", ", typeIds);
        Result<bool> persistResult = await PersistWatchlistAsync(typeIdsText, cancellationToken).ConfigureAwait(false);

        string statusText = $"Built a {typeIds.Count}-item watchlist from saved update-price categories: {string.Join(", ", includedSections)}. Broader legacy category imports still remain deferred in the modern Market tab.";
        if (persistResult.IsFailure)
        {
            statusText = $"{statusText} Workspace settings could not be saved: {persistResult.Error.Message}";
        }

        return Result<MarketPriceWatchlistResult>.Success(new MarketPriceWatchlistResult(typeIdsText, statusText));
    }

    private async Task<Result<bool>> AppendSelectedItemsAsync(
        List<ItemRecord> items,
        UpdatePriceTabSettingsModel settings,
        bool includeAllRawMaterials,
        List<string> includedSections,
        CancellationToken cancellationToken)
    {
        Result<bool> appendResult = Result<bool>.Success(true);

        if (includeAllRawMaterials || settings.Minerals)
        {
            appendResult = await AppendGroupItemsAsync(items, ["Mineral"], "Minerals", cancellationToken).ConfigureAwait(false);
            if (appendResult.IsFailure)
            {
                return appendResult;
            }

            includedSections.Add("Minerals");
        }

        if (includeAllRawMaterials || settings.Gas)
        {
            appendResult = await AppendGroupItemsAsync(items, GasGroupNames, "Gas", cancellationToken).ConfigureAwait(false);
            if (appendResult.IsFailure)
            {
                return appendResult;
            }

            includedSections.Add("Gas");
        }

        if (includeAllRawMaterials || settings.IceProducts)
        {
            appendResult = await AppendGroupItemsAsync(items, ["Ice Product"], "Ice products", cancellationToken).ConfigureAwait(false);
            if (appendResult.IsFailure)
            {
                return appendResult;
            }

            includedSections.Add("Ice products");
        }

        if (includeAllRawMaterials || settings.Planetary)
        {
            appendResult = await AppendCategoryPrefixItemsAsync(items, "Planetary", "Planetary", cancellationToken).ConfigureAwait(false);
            if (appendResult.IsFailure)
            {
                return appendResult;
            }

            includedSections.Add("Planetary");
        }

        if (includeAllRawMaterials || settings.RawMaterials)
        {
            appendResult = await AppendGroupItemsAsync(items, RawMaterialGroupNames, "Raw materials", cancellationToken).ConfigureAwait(false);
            if (appendResult.IsFailure)
            {
                return appendResult;
            }

            appendResult = await AppendCategoryPrefixItemsAsync(items, "Asteroid", "Raw materials", cancellationToken).ConfigureAwait(false);
            if (appendResult.IsFailure)
            {
                return appendResult;
            }

            includedSections.Add("Raw materials");
        }

        if (includeAllRawMaterials || settings.Salvage)
        {
            appendResult = await AppendGroupItemsAsync(items, SalvageGroupNames, "Salvage", cancellationToken).ConfigureAwait(false);
            if (appendResult.IsFailure)
            {
                return appendResult;
            }

            includedSections.Add("Salvage");
        }

        if (settings.AdvancedComponents)
        {
            appendResult = await AppendGroupItemsAsync(items, AdvancedComponentGroupNames, "Advanced components", cancellationToken).ConfigureAwait(false);
            if (appendResult.IsFailure)
            {
                return appendResult;
            }

            includedSections.Add("Advanced components");
        }

        if (settings.FuelBlocks)
        {
            appendResult = await AppendGroupItemsAsync(items, FuelBlockGroupNames, "Fuel blocks", cancellationToken).ConfigureAwait(false);
            if (appendResult.IsFailure)
            {
                return appendResult;
            }

            includedSections.Add("Fuel blocks");
        }

        return Result<bool>.Success(true);
    }

    private async Task<Result<bool>> AppendGroupItemsAsync(
        List<ItemRecord> items,
        IReadOnlyCollection<string> groupNames,
        string failureContext,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ItemRecord>> groupItems = await _itemRepository
            .GetItemsByGroupNamesAsync(groupNames, cancellationToken)
            .ConfigureAwait(false);

        if (groupItems.IsFailure)
        {
            return Result<bool>.Failure(
                $"GROUP_LOOKUP_FAILED_{failureContext.Replace(' ', '_').ToUpperInvariant()}",
                $"Unable to resolve the saved {failureContext.ToLowerInvariant()} selection: {groupItems.Error.Message}");
        }

        items.AddRange(groupItems.Value);
        return Result<bool>.Success(true);
    }

    private async Task<Result<bool>> AppendCategoryPrefixItemsAsync(
        List<ItemRecord> items,
        string categoryPrefix,
        string failureContext,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ItemRecord>> categoryItems = await _itemRepository
            .GetItemsByCategoryPrefixAsync(categoryPrefix, cancellationToken)
            .ConfigureAwait(false);

        if (categoryItems.IsFailure)
        {
            return Result<bool>.Failure(
                $"CATEGORY_LOOKUP_FAILED_{failureContext.Replace(' ', '_').ToUpperInvariant()}",
                $"Unable to resolve the saved {failureContext.ToLowerInvariant()} selection: {categoryItems.Error.Message}");
        }

        items.AddRange(categoryItems.Value);
        return Result<bool>.Success(true);
    }

    private async Task<Maybe<UpdatePriceTabSettingsModel>> ReadUpdatePriceSettingsAsync(CancellationToken cancellationToken)
    {
        return await _settingsStore
            .ReadAsync<UpdatePriceTabSettingsModel>(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<Result<bool>> PersistWatchlistAsync(string typeIdsText, CancellationToken cancellationToken)
    {
        Maybe<UpdatePriceTabSettingsModel> existingSettings = await ReadUpdatePriceSettingsAsync(cancellationToken).ConfigureAwait(false);
        UpdatePriceTabSettingsModel updatedSettings = (existingSettings.HasValue ? existingSettings.Value : new UpdatePriceTabSettingsModel()) with
        {
            ModernMarketTypeIds = typeIdsText,
        };

        return await _settingsStore
            .WriteAsync(updatedSettings, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<Result<bool>> PersistWorkspaceSettingsAsync(
        long regionId,
        string typeIdsText,
        MarketPriceSourceKind sourceKind,
        CancellationToken cancellationToken)
    {
        Maybe<UpdatePriceTabSettingsModel> existingSettings = await ReadUpdatePriceSettingsAsync(cancellationToken).ConfigureAwait(false);

        UpdatePriceTabSettingsModel updatedSettings = (existingSettings.HasValue ? existingSettings.Value : new UpdatePriceTabSettingsModel()) with
        {
            ModernMarketRegionId = regionId,
            ModernMarketTypeIds = typeIdsText,
            PriceDataSource = (int)sourceKind,
        };

        return await _settingsStore
            .WriteAsync(updatedSettings, cancellationToken)
            .ConfigureAwait(false);
    }

    private static Result<IReadOnlyList<TypeId>> ParseTypeIds(string typeIdsText)
    {
        if (string.IsNullOrWhiteSpace(typeIdsText))
        {
            return Result<IReadOnlyList<TypeId>>.Failure("EMPTY_TYPE_IDS", "Enter at least one item type ID.");
        }

        long[] parsed = typeIdsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => long.TryParse(value, out long typeId) ? typeId : 0)
            .Where(typeId => typeId > 0)
            .Distinct()
            .ToArray();

        if (parsed.Length == 0)
        {
            return Result<IReadOnlyList<TypeId>>.Failure("INVALID_TYPE_IDS", "No valid numeric item type IDs were found.");
        }

        return Result<IReadOnlyList<TypeId>>.Success(parsed.Select(typeId => new TypeId(typeId)).ToArray());
    }
}