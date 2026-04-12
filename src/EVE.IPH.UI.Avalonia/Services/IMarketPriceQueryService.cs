using EVE.IPH.Domain.Core.Interfaces;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IMarketPriceQueryService
{
    Task<MarketPriceScreenData> GetScreenDataAsync(CancellationToken cancellationToken = default);
}

public sealed record MarketPriceScreenData(
    long RegionId,
    string TypeIdsText,
    MarketPriceSourceKind SelectedSource,
    IReadOnlyList<MarketPriceSourceOption> SourceOptions,
    string StatusText);

public sealed record MarketPriceSourceOption(MarketPriceSourceKind SourceKind, string DisplayName);