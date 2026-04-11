namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingSaleAdjustmentResult(
    double SalesTaxAmount,
    double BrokerFeeAmount,
    double NetSaleAmount);