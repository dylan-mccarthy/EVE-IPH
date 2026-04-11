using EVE.IPH.Domain.Core.Interfaces;

namespace EVE.IPH.Infrastructure.ESI.Market;

public sealed class MarketPriceSourceResolver(IEnumerable<IMarketPriceSource> sources) : IMarketPriceSourceResolver
{
    private readonly IReadOnlyDictionary<MarketPriceSourceKind, IMarketPriceSource> _sources = (sources ?? throw new ArgumentNullException(nameof(sources)))
        .GroupBy(source => source.SourceKind)
        .ToDictionary(group => group.Key, group => group.Last());

    public IMarketPriceSource Resolve(MarketPriceSourceKind sourceKind)
    {
        if (_sources.TryGetValue(sourceKind, out IMarketPriceSource? source))
        {
            return source;
        }

        throw new InvalidOperationException($"No market price source is registered for '{sourceKind}'.");
    }
}