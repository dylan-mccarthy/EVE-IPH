using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Settings.Models;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class MarketPriceQueryService(
    IMarketPriceSourcePreferenceProvider marketPriceSourcePreferenceProvider,
    ISettingsStore settingsStore) : IMarketPriceQueryService
{
    private const long DefaultRegionId = 10000002;
    private const string DefaultTypeIds = "34, 35, 36, 37";

    private readonly IMarketPriceSourcePreferenceProvider _marketPriceSourcePreferenceProvider = marketPriceSourcePreferenceProvider ?? throw new ArgumentNullException(nameof(marketPriceSourcePreferenceProvider));
    private readonly ISettingsStore _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));

    public async Task<MarketPriceScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default)
    {
        Maybe<UpdatePriceTabSettingsModel> settings = await _settingsStore
            .ReadAsync<UpdatePriceTabSettingsModel>(cancellationToken)
            .ConfigureAwait(false);

        Result<MarketPriceSourceKind> selectedSource = await _marketPriceSourcePreferenceProvider
            .GetSelectedSourceAsync(cancellationToken)
            .ConfigureAwait(false);

        MarketPriceSourceKind sourceKind = selectedSource.IsSuccess ? selectedSource.Value : MarketPriceSourceKind.Fuzzworks;
        long regionId = settings.HasValue && settings.Value.ModernMarketRegionId > 0
            ? settings.Value.ModernMarketRegionId
            : DefaultRegionId;
        string typeIdsText = settings.HasValue && !string.IsNullOrWhiteSpace(settings.Value.ModernMarketTypeIds)
            ? settings.Value.ModernMarketTypeIds
            : DefaultTypeIds;
        string statusText = selectedSource.IsSuccess
            ? "Enter item type IDs and a region ID to load live market prices through the modern market service, or build a watchlist from the saved supported update-price categories. The broader legacy update/download workflow still remains explicitly deferred."
            : $"Using Fuzzworks as the fallback market source because the saved source preference could not be read: {selectedSource.Error.Message}";

        return new MarketPriceScreenData(
            regionId,
            typeIdsText,
            sourceKind,
            [
                new MarketPriceSourceOption(MarketPriceSourceKind.Tranquility, "Tranquility"),
                new MarketPriceSourceOption(MarketPriceSourceKind.EveMarketer, "EVE Marketer"),
                new MarketPriceSourceOption(MarketPriceSourceKind.Fuzzworks, "Fuzzworks"),
            ],
            statusText);
    }
}