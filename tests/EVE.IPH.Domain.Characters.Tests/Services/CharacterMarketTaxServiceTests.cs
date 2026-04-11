using EVE.IPH.Domain.Characters.Services;

namespace EVE.IPH.Domain.Characters.Tests.Services;

public sealed class CharacterMarketTaxServiceTests
{
    [Fact]
    public void GetSalesTaxRatePercent_AppliesAccountingReduction()
    {
        CharacterMarketTaxService service = new();

        double result = service.GetSalesTaxRatePercent(accountingLevel: 5, baseSalesTaxRatePercent: 8D);

        result.Should().BeApproximately(3.6D, 0.001D);
    }

    [Fact]
    public void GetSalesTaxAmount_WithPositivePrice_ReturnsTaxAmount()
    {
        CharacterMarketTaxService service = new();

        double result = service.GetSalesTaxAmount(itemMarketCost: 1_000_000D, accountingLevel: 5, baseSalesTaxRatePercent: 8D);

        result.Should().BeApproximately(36_000D, 0.01D);
    }

    [Fact]
    public void GetBrokerFeeRatePercent_AppliesSkillAndStandingModifiers()
    {
        CharacterMarketTaxService service = new();

        double result = service.GetBrokerFeeRatePercent(
            brokerRelationsLevel: 5,
            factionStanding: 7D,
            corporationStanding: 8D,
            baseBrokerFeeRatePercent: 3D);

        result.Should().BeApproximately(1.13D, 0.001D);
    }

    [Fact]
    public void GetBrokerFeeAmount_WhenComputedFeeIsBelowMinimum_ReturnsMinimumFee()
    {
        CharacterMarketTaxService service = new();

        double result = service.GetBrokerFeeAmount(
            itemMarketCost: 5_000D,
            brokerRelationsLevel: 5,
            factionStanding: 7D,
            corporationStanding: 8D,
            baseBrokerFeeRatePercent: 3D);

        result.Should().Be(100D);
    }

    [Fact]
    public void GetBrokerFeeAmount_WhenComputedFeeExceedsMinimum_ReturnsComputedFee()
    {
        CharacterMarketTaxService service = new();

        double result = service.GetBrokerFeeAmount(
            itemMarketCost: 1_000_000D,
            brokerRelationsLevel: 5,
            factionStanding: 7D,
            corporationStanding: 8D,
            baseBrokerFeeRatePercent: 3D);

        result.Should().BeApproximately(11_300D, 0.01D);
    }
}