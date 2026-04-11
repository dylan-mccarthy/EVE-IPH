using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Settings.Models;

namespace EVE.IPH.Infrastructure.Settings;

public sealed class UpdatePriceSettingsMarketPriceSourcePreferenceProvider(ISettingsStore settingsStore) : IMarketPriceSourcePreferenceProvider
{
    private readonly ISettingsStore _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));

    public async Task<Result<MarketPriceSourceKind>> GetSelectedSourceAsync(CancellationToken cancellationToken = default)
    {
        Maybe<UpdatePriceTabSettingsModel> settings = await _settingsStore
            .ReadAsync<UpdatePriceTabSettingsModel>(cancellationToken)
            .ConfigureAwait(false);

        if (!settings.HasValue)
        {
            return Result<MarketPriceSourceKind>.Success(MarketPriceSourceKind.Fuzzworks);
        }

        if (!Enum.IsDefined(typeof(MarketPriceSourceKind), settings.Value.PriceDataSource))
        {
            return Result<MarketPriceSourceKind>.Failure(
                "INVALID_PRICE_SOURCE",
                $"Unsupported market price source value '{settings.Value.PriceDataSource}'.");
        }

        return Result<MarketPriceSourceKind>.Success((MarketPriceSourceKind)settings.Value.PriceDataSource);
    }
}