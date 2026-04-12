using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IMarketPriceCommandService
{
    Task<Result<MarketPriceResult>> LoadPricesAsync(MarketPriceRequest request, CancellationToken cancellationToken = default);
    Task<Result<MarketPriceWatchlistResult>> BuildWatchlistFromSavedSelectionAsync(CancellationToken cancellationToken = default);
}

public sealed record MarketPriceRequest(
    long RegionId,
    string TypeIdsText,
    MarketPriceSourceKind SourceKind);

public sealed record MarketPriceResult(
    IReadOnlyList<MarketPriceRow> Rows,
    string StatusText);

public sealed record MarketPriceWatchlistResult(
    string TypeIdsText,
    string StatusText);

public sealed record MarketPriceRow(
    long TypeId,
    string ItemName,
    double? MinSell,
    double? MaxBuy,
    double? Average)
{
    public string DisplayName => string.IsNullOrWhiteSpace(ItemName) ? TypeId.ToString() : ItemName;
}