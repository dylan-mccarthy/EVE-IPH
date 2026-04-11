namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingSaleAdjustmentInput(
    bool IncludeSale,
    double GrossSaleAmount,
    bool ApplySalesTax,
    double BaseSalesTaxRatePercent,
    int AccountingSkillLevel,
    SalesBrokerFeeMode BrokerFeeMode,
    double BaseBrokerFeeRatePercent,
    int BrokerRelationsSkillLevel,
    double BrokerFactionStanding,
    double BrokerCorporationStanding,
    double FixedBrokerFeeRate,
    double SccBrokerFeeSurcharge);