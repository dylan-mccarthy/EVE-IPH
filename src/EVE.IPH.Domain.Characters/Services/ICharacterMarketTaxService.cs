namespace EVE.IPH.Domain.Characters.Services;

/// <summary>
/// Computes market taxes and broker fees derived from character skills and standings.
/// </summary>
public interface ICharacterMarketTaxService
{
    double GetSalesTaxRatePercent(int accountingLevel, double baseSalesTaxRatePercent);

    double GetSalesTaxAmount(double itemMarketCost, int accountingLevel, double baseSalesTaxRatePercent);

    double GetBrokerFeeRatePercent(int brokerRelationsLevel, double factionStanding, double corporationStanding, double baseBrokerFeeRatePercent);

    double GetBrokerFeeAmount(double itemMarketCost, int brokerRelationsLevel, double factionStanding, double corporationStanding, double baseBrokerFeeRatePercent);
}