using EVE.IPH.Domain.Manufacturing.Models;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class ManufacturingProfitabilityCalculatorTests
{
    [Fact]
    public void Calculate_WhenNotBuildBuy_ReturnsLegacyRawAndComponentProfitability()
    {
        ManufacturingProfitabilityInput input = new(
            ItemMarketCost: 1_000_000,
            RawMaterialsCost: 400_000,
            ComponentMaterialsCost: 500_000,
            InventionCost: 20_000,
            CopyCost: 10_000,
            TaxesAndFees: 30_000,
            AdditionalCosts: 5_000,
            TotalUsage: 40_000,
            ComponentUsage: 10_000,
            RemainingReactionUsage: 5_000,
            ReprocessingUsage: 2_000,
            SellExcessAmount: 8_000,
            TotalProductionTimeSeconds: 7_200,
            BlueprintProductionTimeSeconds: 3_600,
            IsBuildBuy: false);

        ManufacturingProfitabilityCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRawCost.Should().Be(497_000);
        result.Value.TotalComponentCost.Should().Be(588_000);
        result.Value.TotalRawProfit.Should().Be(503_000);
        result.Value.TotalComponentProfit.Should().Be(412_000);
        result.Value.TotalRawProfitPercent.Should().BeApproximately(0.503d, 0.000001d);
        result.Value.TotalComponentProfitPercent.Should().BeApproximately(0.412d, 0.000001d);
        result.Value.TotalIskPerHourRaw.Should().BeApproximately(251_500d, 0.000001d);
        result.Value.TotalIskPerHourComponent.Should().BeApproximately(412_000d, 0.000001d);
    }

    [Fact]
    public void Calculate_WhenBuildBuy_UsesRawIskPerHourAndReaddsComponentUsage()
    {
        ManufacturingProfitabilityInput input = new(
            ItemMarketCost: 1_000_000,
            RawMaterialsCost: 400_000,
            ComponentMaterialsCost: 500_000,
            InventionCost: 20_000,
            CopyCost: 10_000,
            TaxesAndFees: 30_000,
            AdditionalCosts: 5_000,
            TotalUsage: 40_000,
            ComponentUsage: 10_000,
            RemainingReactionUsage: 5_000,
            ReprocessingUsage: 2_000,
            SellExcessAmount: 8_000,
            TotalProductionTimeSeconds: 7_200,
            BlueprintProductionTimeSeconds: 3_600,
            IsBuildBuy: true);

        ManufacturingProfitabilityCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalComponentCost.Should().Be(597_000);
        result.Value.TotalComponentProfit.Should().Be(403_000);
        result.Value.TotalIskPerHourRaw.Should().BeApproximately(251_500d, 0.000001d);
        result.Value.TotalIskPerHourComponent.Should().BeApproximately(251_500d, 0.000001d);
    }

    [Fact]
    public void Calculate_WhenItemMarketCostIsZero_ReturnsZeroProfitPercents()
    {
        ManufacturingProfitabilityInput input = new(
            ItemMarketCost: 0,
            RawMaterialsCost: 100,
            ComponentMaterialsCost: 100,
            InventionCost: 0,
            CopyCost: 0,
            TaxesAndFees: 0,
            AdditionalCosts: 0,
            TotalUsage: 0,
            ComponentUsage: 0,
            RemainingReactionUsage: 0,
            ReprocessingUsage: 0,
            SellExcessAmount: 0,
            TotalProductionTimeSeconds: 60,
            BlueprintProductionTimeSeconds: 60,
            IsBuildBuy: false);

        ManufacturingProfitabilityCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRawProfitPercent.Should().Be(0);
        result.Value.TotalComponentProfitPercent.Should().Be(0);
    }

    [Fact]
    public void Calculate_WhenTotalProductionTimeIsInvalid_ReturnsFailure()
    {
        ManufacturingProfitabilityInput input = new(
            ItemMarketCost: 1,
            RawMaterialsCost: 1,
            ComponentMaterialsCost: 1,
            InventionCost: 0,
            CopyCost: 0,
            TaxesAndFees: 0,
            AdditionalCosts: 0,
            TotalUsage: 0,
            ComponentUsage: 0,
            RemainingReactionUsage: 0,
            ReprocessingUsage: 0,
            SellExcessAmount: 0,
            TotalProductionTimeSeconds: 0,
            BlueprintProductionTimeSeconds: 1,
            IsBuildBuy: false);

        ManufacturingProfitabilityCalculator sut = new();

        var result = sut.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_TOTAL_PRODUCTION_TIME");
    }
}