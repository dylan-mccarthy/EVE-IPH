using EVE.IPH.Domain.Manufacturing.Models;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class ManufacturingSaleAdjustmentCalculatorTests
{
    private readonly ManufacturingSaleAdjustmentCalculator _sut = new();

    [Fact]
    public void Calculate_WhenSaleExcluded_ReturnsZeroes()
    {
        ManufacturingSaleAdjustmentInput input = new(false, 10_000, false, 0, 0, SalesBrokerFeeMode.NoFee, 0, 0, 0, 0, 0, 0);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.NetSaleAmount.Should().Be(0);
    }

    [Fact]
    public void Calculate_WhenUsingStandardTaxAndBrokerFee_ReturnsLegacyAdjustedPrice()
    {
        ManufacturingSaleAdjustmentInput input = new(
            IncludeSale: true,
            GrossSaleAmount: 10_000,
            ApplySalesTax: true,
            BaseSalesTaxRatePercent: 8,
            AccountingSkillLevel: 5,
            BrokerFeeMode: SalesBrokerFeeMode.Fee,
            BaseBrokerFeeRatePercent: 3,
            BrokerRelationsSkillLevel: 5,
            BrokerFactionStanding: 5,
            BrokerCorporationStanding: 5,
            FixedBrokerFeeRate: 0,
            SccBrokerFeeSurcharge: 0);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.SalesTaxAmount.Should().BeApproximately(360d, 0.000001d);
        result.Value.BrokerFeeAmount.Should().BeApproximately(125d, 0.000001d);
        result.Value.NetSaleAmount.Should().BeApproximately(9_515d, 0.000001d);
    }

    [Fact]
    public void Calculate_WhenUsingSpecialBrokerFee_AddsSccSurcharge()
    {
        ManufacturingSaleAdjustmentInput input = new(
            IncludeSale: true,
            GrossSaleAmount: 20_000,
            ApplySalesTax: false,
            BaseSalesTaxRatePercent: 0,
            AccountingSkillLevel: 0,
            BrokerFeeMode: SalesBrokerFeeMode.SpecialFee,
            BaseBrokerFeeRatePercent: 0,
            BrokerRelationsSkillLevel: 0,
            BrokerFactionStanding: 0,
            BrokerCorporationStanding: 0,
            FixedBrokerFeeRate: 0.01,
            SccBrokerFeeSurcharge: 0.015);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.BrokerFeeAmount.Should().BeApproximately(500d, 0.000001d);
        result.Value.NetSaleAmount.Should().BeApproximately(19_500d, 0.000001d);
    }

    [Fact]
    public void Calculate_WhenComputedBrokerFeeFallsBelowMinimum_UsesMinimumFee()
    {
        ManufacturingSaleAdjustmentInput input = new(
            IncludeSale: true,
            GrossSaleAmount: 1_000,
            ApplySalesTax: false,
            BaseSalesTaxRatePercent: 0,
            AccountingSkillLevel: 0,
            BrokerFeeMode: SalesBrokerFeeMode.Fee,
            BaseBrokerFeeRatePercent: 3,
            BrokerRelationsSkillLevel: 5,
            BrokerFactionStanding: 10,
            BrokerCorporationStanding: 10,
            FixedBrokerFeeRate: 0,
            SccBrokerFeeSurcharge: 0);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.BrokerFeeAmount.Should().Be(100d);
        result.Value.NetSaleAmount.Should().Be(900d);
    }
}