namespace EVE.IPH.Domain.Characters.Services;

/// <summary>
/// Implements the legacy sales-tax and broker-fee formulas used by the application.
/// </summary>
public sealed class CharacterMarketTaxService : ICharacterMarketTaxService
{
    public double GetSalesTaxRatePercent(int accountingLevel, double baseSalesTaxRatePercent) =>
        baseSalesTaxRatePercent - (accountingLevel * 0.11 * baseSalesTaxRatePercent);

    public double GetSalesTaxAmount(double itemMarketCost, int accountingLevel, double baseSalesTaxRatePercent)
    {
        if (itemMarketCost <= 0)
        {
            return 0;
        }

        return (GetSalesTaxRatePercent(accountingLevel, baseSalesTaxRatePercent) / 100D) * itemMarketCost;
    }

    public double GetBrokerFeeRatePercent(int brokerRelationsLevel, double factionStanding, double corporationStanding, double baseBrokerFeeRatePercent) =>
        baseBrokerFeeRatePercent - (0.3 * brokerRelationsLevel) - (0.03 * factionStanding) - (0.02 * corporationStanding);

    public double GetBrokerFeeAmount(double itemMarketCost, int brokerRelationsLevel, double factionStanding, double corporationStanding, double baseBrokerFeeRatePercent)
    {
        if (itemMarketCost <= 0)
        {
            return 0;
        }

        double brokerFee = (GetBrokerFeeRatePercent(brokerRelationsLevel, factionStanding, corporationStanding, baseBrokerFeeRatePercent) / 100D) * itemMarketCost;
        return brokerFee < 100D ? 100D : brokerFee;
    }
}