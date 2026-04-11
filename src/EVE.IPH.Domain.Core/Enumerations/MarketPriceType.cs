namespace EVE.IPH.Domain.Core.Enumerations;

/// <summary>
/// The market price point to use when valuing an item.
/// Maps to the "Min Sell" / "Max Buy" / "Average" concepts in the legacy application.
/// </summary>
public enum MarketPriceType
{
    /// <summary>Lowest sell-order price (buy from seller). Equivalent to legacy "Min Sell".</summary>
    MinSell,

    /// <summary>Highest buy-order price (sell to buyer). Equivalent to legacy "Max Buy".</summary>
    MaxBuy,

    /// <summary>Daily volume-weighted average price from ESI market history.</summary>
    Average,

    /// <summary>The Jita IV - Moon 4 station minimum sell price.</summary>
    Jita4MinSell,

    /// <summary>The Jita IV - Moon 4 station maximum buy price.</summary>
    Jita4MaxBuy,
}
