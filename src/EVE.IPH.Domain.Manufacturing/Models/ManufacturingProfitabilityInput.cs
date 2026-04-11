namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingProfitabilityInput(
    double ItemMarketCost,
    double RawMaterialsCost,
    double ComponentMaterialsCost,
    double InventionCost,
    double CopyCost,
    double TaxesAndFees,
    double AdditionalCosts,
    double TotalUsage,
    double ComponentUsage,
    double RemainingReactionUsage,
    double ReprocessingUsage,
    double SellExcessAmount,
    double TotalProductionTimeSeconds,
    double BlueprintProductionTimeSeconds,
    bool IsBuildBuy);