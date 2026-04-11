namespace EVE.IPH.Domain.Core.Interfaces;

/// <summary>
/// Supported market price providers. Values match the legacy cache source IDs.
/// </summary>
public enum MarketPriceSourceKind
{
    Tranquility = 0,
    EveMarketer = 1,
    Fuzzworks = 2,
}