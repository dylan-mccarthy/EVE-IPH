namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Resolves a concrete market price source for the requested provider kind.
/// </summary>
public interface IMarketPriceSourceResolver
{
    IMarketPriceSource Resolve(MarketPriceSourceKind sourceKind);
}