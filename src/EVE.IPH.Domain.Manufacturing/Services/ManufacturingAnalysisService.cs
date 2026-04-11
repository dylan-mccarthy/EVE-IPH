using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Models;

namespace EVE.IPH.Domain.Manufacturing.Services;

public sealed class ManufacturingAnalysisService(
    ManufacturingPrerequisiteService prerequisiteService,
    ManufacturingFacilityUsageCalculator facilityUsageCalculator,
    ManufacturingUsageAllocationCalculator usageAllocationCalculator,
    ManufacturingSaleAdjustmentCalculator saleAdjustmentCalculator,
    ManufacturingBuildBuyDecider buildBuyDecider,
    ManufacturingActivityCalculator activityCalculator,
    ComponentProductionScheduleCalculator componentScheduleCalculator,
    ManufacturingCostCalculator costCalculator,
    ManufacturingTimelineCalculator timelineCalculator,
    ManufacturingProfitabilityCalculator profitabilityCalculator)
{
    private readonly ManufacturingPrerequisiteService _prerequisiteService = prerequisiteService;
    private readonly ManufacturingFacilityUsageCalculator _facilityUsageCalculator = facilityUsageCalculator;
    private readonly ManufacturingUsageAllocationCalculator _usageAllocationCalculator = usageAllocationCalculator;
    private readonly ManufacturingSaleAdjustmentCalculator _saleAdjustmentCalculator = saleAdjustmentCalculator;
    private readonly ManufacturingBuildBuyDecider _buildBuyDecider = buildBuyDecider;
    private readonly ManufacturingActivityCalculator _activityCalculator = activityCalculator;
    private readonly ComponentProductionScheduleCalculator _componentScheduleCalculator = componentScheduleCalculator;
    private readonly ManufacturingCostCalculator _costCalculator = costCalculator;
    private readonly ManufacturingTimelineCalculator _timelineCalculator = timelineCalculator;
    private readonly ManufacturingProfitabilityCalculator _profitabilityCalculator = profitabilityCalculator;

    public Result<ManufacturingAnalysisResult> Calculate(ManufacturingAnalysisInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Prerequisites);
        ArgumentNullException.ThrowIfNull(input.FacilityUsage);
        ArgumentNullException.ThrowIfNull(input.UsageAllocation);
        ArgumentNullException.ThrowIfNull(input.ItemSaleCharges);
        ArgumentNullException.ThrowIfNull(input.SellExcessAdjustment);
        ArgumentNullException.ThrowIfNull(input.BuildBuy);
        ArgumentNullException.ThrowIfNull(input.Activity);
        ArgumentNullException.ThrowIfNull(input.ComponentSchedule);

        Result<ManufacturingPrerequisiteResult> prerequisiteResult = _prerequisiteService.Calculate(input.Prerequisites);
        if (prerequisiteResult.IsFailure)
        {
            return Result<ManufacturingAnalysisResult>.Failure(prerequisiteResult.Error);
        }

        Result<ManufacturingFacilityUsageResult> facilityUsageResult = _facilityUsageCalculator.Calculate(input.FacilityUsage);
        if (facilityUsageResult.IsFailure)
        {
            return Result<ManufacturingAnalysisResult>.Failure(facilityUsageResult.Error);
        }

        ManufacturingUsageAllocationInput usageAllocationInput = input.UsageAllocation with
        {
            FacilityUsage = facilityUsageResult.Value,
        };

        Result<ManufacturingUsageAllocationResult> usageAllocationResult = _usageAllocationCalculator.Calculate(usageAllocationInput);
        if (usageAllocationResult.IsFailure)
        {
            return Result<ManufacturingAnalysisResult>.Failure(usageAllocationResult.Error);
        }

        Result<ManufacturingSaleAdjustmentResult> itemSaleChargesResult = _saleAdjustmentCalculator.Calculate(input.ItemSaleCharges);
        if (itemSaleChargesResult.IsFailure)
        {
            return Result<ManufacturingAnalysisResult>.Failure(itemSaleChargesResult.Error);
        }

        Result<ManufacturingSaleAdjustmentResult> saleAdjustmentResult = _saleAdjustmentCalculator.Calculate(input.SellExcessAdjustment);
        if (saleAdjustmentResult.IsFailure)
        {
            return Result<ManufacturingAnalysisResult>.Failure(saleAdjustmentResult.Error);
        }

        ManufacturingBuildBuyInput buildBuyInput = input.BuildBuy with
        {
            NetExcessSaleAmount = saleAdjustmentResult.Value.NetSaleAmount,
        };

        Result<ManufacturingBuildBuyResult> buildBuyResult = _buildBuyDecider.Calculate(buildBuyInput);
        if (buildBuyResult.IsFailure)
        {
            return Result<ManufacturingAnalysisResult>.Failure(buildBuyResult.Error);
        }

        Result<ManufacturingActivityResult> activityResult = _activityCalculator.Calculate(input.Activity);
        if (activityResult.IsFailure)
        {
            return Result<ManufacturingAnalysisResult>.Failure(activityResult.Error);
        }

        Result<ComponentProductionScheduleResult> componentScheduleResult = _componentScheduleCalculator.Calculate(input.ComponentSchedule);
        if (componentScheduleResult.IsFailure)
        {
            return Result<ManufacturingAnalysisResult>.Failure(componentScheduleResult.Error);
        }

        ManufacturingCostInput costInput = new(
            input.IsTech3,
            input.IncludeInventionCosts,
            input.IncludeCopyCosts,
            input.UserRuns,
            input.TotalInventedRuns,
            input.PerInventionRunCost,
            input.TotalCopyCost,
            usageAllocationResult.Value.MainFacilityUsage,
            usageAllocationResult.Value.ComponentUsage,
            activityResult.Value.InventionUsagePerRun,
            activityResult.Value.CopyUsagePerRun,
            usageAllocationResult.Value.RemainingReactionUsage,
            usageAllocationResult.Value.ReprocessingUsage);

        Result<ManufacturingCostResult> costResult = _costCalculator.Calculate(costInput);
        if (costResult.IsFailure)
        {
            return Result<ManufacturingAnalysisResult>.Failure(costResult.Error);
        }

        ManufacturingTimelineInput timelineInput = new(
            input.BaseBlueprintProductionTimeSeconds,
            activityResult.Value.CopyTimeSeconds,
            activityResult.Value.InventionTimeSeconds,
            componentScheduleResult.Value.TotalComponentProductionTimeSeconds);

        Result<ManufacturingTimelineResult> timelineResult = _timelineCalculator.Calculate(timelineInput);
        if (timelineResult.IsFailure)
        {
            return Result<ManufacturingAnalysisResult>.Failure(timelineResult.Error);
        }

        ManufacturingProfitabilityInput profitabilityInput = new(
            input.ItemMarketCost,
            input.RawMaterialsCost,
            input.ComponentMaterialsCost,
            costResult.Value.InventionCost,
            costResult.Value.CopyCost,
            itemSaleChargesResult.Value.SalesTaxAmount + itemSaleChargesResult.Value.BrokerFeeAmount,
            input.AdditionalCosts,
            costResult.Value.TotalUsage,
            usageAllocationResult.Value.ComponentUsage,
            usageAllocationResult.Value.RemainingReactionUsage,
            usageAllocationResult.Value.ReprocessingUsage,
            saleAdjustmentResult.Value.NetSaleAmount,
            timelineResult.Value.TotalProductionTimeSeconds,
            timelineResult.Value.BlueprintProductionTimeSeconds,
            input.IsBuildBuy);

        Result<ManufacturingProfitabilityResult> profitabilityResult = _profitabilityCalculator.Calculate(profitabilityInput);
        if (profitabilityResult.IsFailure)
        {
            return Result<ManufacturingAnalysisResult>.Failure(profitabilityResult.Error);
        }

        return Result<ManufacturingAnalysisResult>.Success(new ManufacturingAnalysisResult(
            prerequisiteResult.Value,
            facilityUsageResult.Value,
            usageAllocationResult.Value,
            itemSaleChargesResult.Value,
            saleAdjustmentResult.Value,
            buildBuyResult.Value,
            activityResult.Value,
            componentScheduleResult.Value,
            costResult.Value,
            timelineResult.Value,
            profitabilityResult.Value));
    }
}