using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Models;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class ManufacturingSaleAdjustmentCalculator
{
    public Result<ManufacturingSaleAdjustmentResult> Calculate(ManufacturingSaleAdjustmentInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.GrossSaleAmount < 0
            || input.BaseSalesTaxRatePercent < 0
            || input.AccountingSkillLevel < 0
            || input.BaseBrokerFeeRatePercent < 0
            || input.BrokerRelationsSkillLevel < 0
            || input.FixedBrokerFeeRate < 0
            || input.SccBrokerFeeSurcharge < 0)
        {
            return Result<ManufacturingSaleAdjustmentResult>.Failure("INVALID_SALE_ADJUSTMENT_INPUT", "Sale adjustment inputs must be valid non-negative values.");
        }

        if (!input.IncludeSale || input.GrossSaleAmount == 0)
        {
            return Result<ManufacturingSaleAdjustmentResult>.Success(new ManufacturingSaleAdjustmentResult(0, 0, 0));
        }

        double salesTaxAmount = 0;
        if (input.ApplySalesTax)
        {
            double effectiveSalesTaxRate = input.BaseSalesTaxRatePercent - (input.AccountingSkillLevel * 0.11d * input.BaseSalesTaxRatePercent);
            salesTaxAmount = (effectiveSalesTaxRate / 100d) * input.GrossSaleAmount;
        }

        double brokerFeeAmount = input.BrokerFeeMode switch
        {
            SalesBrokerFeeMode.Fee => ((input.BaseBrokerFeeRatePercent
                - (0.3d * input.BrokerRelationsSkillLevel)
                - (0.03d * input.BrokerFactionStanding)
                - (0.02d * input.BrokerCorporationStanding)) / 100d) * input.GrossSaleAmount,
            SalesBrokerFeeMode.SpecialFee => (input.FixedBrokerFeeRate * input.GrossSaleAmount) + (input.SccBrokerFeeSurcharge * input.GrossSaleAmount),
            _ => 0,
        };

        if (input.BrokerFeeMode != SalesBrokerFeeMode.NoFee && brokerFeeAmount < 100d)
        {
            brokerFeeAmount = 100d;
        }

        double netSaleAmount = input.GrossSaleAmount - salesTaxAmount - brokerFeeAmount;
        if (netSaleAmount < 0)
        {
            netSaleAmount = 0;
        }

        return Result<ManufacturingSaleAdjustmentResult>.Success(new ManufacturingSaleAdjustmentResult(
            salesTaxAmount,
            brokerFeeAmount,
            netSaleAmount));
    }
}