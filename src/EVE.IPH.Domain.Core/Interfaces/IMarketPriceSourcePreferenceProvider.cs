using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Reads the configured market price provider preference from persisted settings.
/// </summary>
public interface IMarketPriceSourcePreferenceProvider
{
    Task<Result<MarketPriceSourceKind>> GetSelectedSourceAsync(CancellationToken cancellationToken = default);
}