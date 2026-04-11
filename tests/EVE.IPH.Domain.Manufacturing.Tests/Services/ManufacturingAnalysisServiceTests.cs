using EVE.IPH.Domain.Manufacturing.Models;
using EVE.IPH.Domain.Manufacturing.Services;

namespace EVE.IPH.Domain.Manufacturing.Tests.Services;

public sealed class ManufacturingAnalysisServiceTests
{
    private readonly ManufacturingAnalysisService _sut = new(
        new ManufacturingPrerequisiteService(),
        new ManufacturingFacilityUsageCalculator(),
        new ManufacturingUsageAllocationCalculator(),
        new ManufacturingSaleAdjustmentCalculator(),
        new ManufacturingBuildBuyDecider(),
        new ManufacturingActivityCalculator(),
        new ComponentProductionScheduleCalculator(),
        new ManufacturingCostCalculator(),
        new ManufacturingTimelineCalculator(),
        new ManufacturingProfitabilityCalculator());

    [Fact]
    public void Calculate_WithResolvedInputs_ComposesLegacyManufacturingFlow()
    {
        ManufacturingAnalysisInput input = new(
            Prerequisites: new ManufacturingPrerequisiteInput(
            [
                new ManufacturingSkillRequirement(3398, 4),
                new ManufacturingSkillRequirement(81896, 3),
            ],
            new Dictionary<long, int>
            {
                [3398] = 5,
                [81896] = 4,
            }),
            FacilityUsage: new ManufacturingFacilityUsageInput(
                IncludeManufacturingUsage: true,
                EstimatedItemValue: 250,
                UserRuns: 36,
                CostIndex: 0.01,
                CostMultiplier: 1,
                FwManufacturingCostBonus: 1,
                FacilityTaxRate: 0.005,
                SccIndustryFeeSurcharge: 0.005,
                IsAlphaAccount: false,
                AlphaAccountTaxRate: 0,
                IsReactionManufacturing: false,
                HasFulcrumPirateReduction: false),
            UsageAllocation: new ManufacturingUsageAllocationInput(
                new ManufacturingFacilityUsageResult(0, 0, 0, 0, 0),
                IncludeComponentManufacturingUsage: true,
                ComponentFacilityUsage: 60,
                IncludeCapitalComponentManufacturingUsage: true,
                CapitalComponentFacilityUsage: 30,
                IncludeReactionUsage: false,
                TotalReactionFacilityUsage: 45,
                HasReprocessingFacility: true,
                IncludeReprocessingUsage: true,
                ReprocessingUsage: 12),
            ItemSaleCharges: new ManufacturingSaleAdjustmentInput(true, 1_000_000, false, 0, 0, SalesBrokerFeeMode.SpecialFee, 0, 0, 0, 0, 0.02, 0.01),
            SellExcessAdjustment: new ManufacturingSaleAdjustmentInput(true, 8_000, false, 0, 0, SalesBrokerFeeMode.NoFee, 0, 0, 0, 0, 0, 0),
            BuildBuy: new ManufacturingBuildBuyInput("Widget", 100, 2, 5, 900, 0, false, true, true, false, false, false, null),
            Activity: new ManufacturingActivityInput(
                IsTech3: false,
                IncludeCopyUsage: true,
                IncludeInventionUsage: true,
                IncludeCopyTime: true,
                IncludeInventionTime: true,
                TotalInventedRuns: 108,
                NumberOfInventionJobs: 18,
                NumberOfInventionSessions: 2,
                UserCopyRuns: 18,
                EstimatedItemValue: 2_500_000,
                CopyCostIndex: 0.04,
                CopyFwCostBonus: 0.8,
                CopyFacilityCostMultiplier: 0.9,
                CopyFacilityTaxRate: 0.05,
                InventionCostIndex: 0.06,
                InventionFwCostBonus: 0.7,
                InventionFacilityCostMultiplier: 0.85,
                InventionFacilityTaxRate: 0.04,
                BaseCopyTimeSeconds: 1_200,
                BaseInventionTimeSeconds: 7_200,
                ScienceSkillLevel: 5,
                AdvancedIndustrySkillLevel: 5,
                CopyFacilityTimeMultiplier: 0.75,
                InventionFacilityTimeMultiplier: 0.8,
                CopyImplantBonus: 0.02),
            ComponentSchedule: new ComponentProductionScheduleInput([600d, 540d, 480d, 420d, 360d], 2),
            BaseBlueprintProductionTimeSeconds: 28_800,
            IsTech3: false,
            IncludeInventionCosts: true,
            IncludeCopyCosts: true,
            UserRuns: 36,
            TotalInventedRuns: 108,
            PerInventionRunCost: 1_250.5,
            TotalCopyCost: 21_600,
            ItemMarketCost: 1_000_000,
            RawMaterialsCost: 400_000,
            ComponentMaterialsCost: 500_000,
            AdditionalCosts: 5_000,
            IsBuildBuy: false);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
    result.Value.Prerequisites.CanBuild.Should().BeTrue();
    result.Value.Prerequisites.AdvancedManufacturingTimeMultiplier.Should().BeApproximately(0.874d, 0.000001d);
        result.Value.FacilityUsage.MainFacilityUsage.Should().BeApproximately(180d, 0.000001d);
        result.Value.UsageAllocation.ComponentUsage.Should().BeApproximately(90d, 0.000001d);
        result.Value.UsageAllocation.ReprocessingUsage.Should().BeApproximately(12d, 0.000001d);
        result.Value.ItemSaleCharges.BrokerFeeAmount.Should().BeApproximately(30_000d, 0.000001d);
        result.Value.SellExcessAdjustment.NetSaleAmount.Should().BeApproximately(8_000d, 0.000001d);
        result.Value.BuildBuy.CheaperToBuild.Should().BeTrue();
        result.Value.BuildBuy.BuildItem.Should().BeTrue();
        result.Value.Activity.CopyUsagePerRun.Should().BeApproximately(252d, 0.000001d);
        result.Value.ComponentSchedule.TotalComponentProductionTimeSeconds.Should().Be(1980d);
        result.Value.Cost.InventionCost.Should().Be(45_018d);
        result.Value.Cost.CopyCost.Should().Be(7_200d);
        result.Value.Timeline.BlueprintProductionTimeSeconds.Should().BeApproximately(48_712.95d, 0.000001d);
        result.Value.Timeline.TotalProductionTimeSeconds.Should().BeApproximately(50_692.95d, 0.000001d);
        result.Value.Profitability.TotalRawCost.Should().BeApproximately(480_061.4d, 0.000001d);
        result.Value.Profitability.TotalComponentCost.Should().BeApproximately(587_959.4d, 0.000001d);
        result.Value.Profitability.TotalIskPerHourRaw.Should().BeApproximately(36_923.8515414865d, 0.000001d);
        result.Value.Profitability.TotalIskPerHourComponent.Should().BeApproximately(30_450.7561131075d, 0.000001d);
    }

    [Fact]
    public void Calculate_WhenActivityInputIsInvalid_PropagatesFailure()
    {
        ManufacturingAnalysisInput input = new(
            Prerequisites: new ManufacturingPrerequisiteInput([], new Dictionary<long, int>()),
            FacilityUsage: new ManufacturingFacilityUsageInput(false, 0, 1, 0, 1, 1, 0, 0, false, 0, false, false),
            UsageAllocation: new ManufacturingUsageAllocationInput(new ManufacturingFacilityUsageResult(0, 0, 0, 0, 0), false, 0, false, 0, false, 0, false, false, 0),
            ItemSaleCharges: new ManufacturingSaleAdjustmentInput(false, 0, false, 0, 0, SalesBrokerFeeMode.NoFee, 0, 0, 0, 0, 0, 0),
            SellExcessAdjustment: new ManufacturingSaleAdjustmentInput(false, 0, false, 0, 0, SalesBrokerFeeMode.NoFee, 0, 0, 0, 0, 0, 0),
            BuildBuy: new ManufacturingBuildBuyInput("Widget", 1, 1, 1, 1, 0, false, true, false, false, false, false, null),
            Activity: new ManufacturingActivityInput(
                IsTech3: false,
                IncludeCopyUsage: true,
                IncludeInventionUsage: false,
                IncludeCopyTime: false,
                IncludeInventionTime: false,
                TotalInventedRuns: 0,
                NumberOfInventionJobs: 1,
                NumberOfInventionSessions: 0,
                UserCopyRuns: 0,
                EstimatedItemValue: 1,
                CopyCostIndex: 0.01,
                CopyFwCostBonus: 1,
                CopyFacilityCostMultiplier: 1,
                CopyFacilityTaxRate: 0,
                InventionCostIndex: 0.01,
                InventionFwCostBonus: 1,
                InventionFacilityCostMultiplier: 1,
                InventionFacilityTaxRate: 0,
                BaseCopyTimeSeconds: 1,
                BaseInventionTimeSeconds: 1,
                ScienceSkillLevel: 0,
                AdvancedIndustrySkillLevel: 0,
                CopyFacilityTimeMultiplier: 1,
                InventionFacilityTimeMultiplier: 1,
                CopyImplantBonus: 0),
            ComponentSchedule: new ComponentProductionScheduleInput([], 1),
            BaseBlueprintProductionTimeSeconds: 1,
            IsTech3: false,
            IncludeInventionCosts: false,
            IncludeCopyCosts: false,
            UserRuns: 1,
            TotalInventedRuns: 0,
            PerInventionRunCost: 0,
            TotalCopyCost: 0,
            ItemMarketCost: 1,
            RawMaterialsCost: 1,
            ComponentMaterialsCost: 1,
            AdditionalCosts: 0,
            IsBuildBuy: false);

        var result = _sut.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_TOTAL_INVENTED_RUNS");
    }

    [Fact]
    public void Calculate_WhenBuildBuy_ComposesRawIskPerHourForComponentView()
    {
        ManufacturingAnalysisInput input = new(
            Prerequisites: new ManufacturingPrerequisiteInput(
            [
                new ManufacturingSkillRequirement(3398, 5),
            ],
            new Dictionary<long, int>
            {
                [3398] = 4,
            }),
            FacilityUsage: new ManufacturingFacilityUsageInput(
                IncludeManufacturingUsage: true,
                EstimatedItemValue: 100,
                UserRuns: 10,
                CostIndex: 0.005,
                CostMultiplier: 1,
                FwManufacturingCostBonus: 1,
                FacilityTaxRate: 0.005,
                SccIndustryFeeSurcharge: 0,
                IsAlphaAccount: false,
                AlphaAccountTaxRate: 0,
                IsReactionManufacturing: false,
                HasFulcrumPirateReduction: false),
            UsageAllocation: new ManufacturingUsageAllocationInput(
                new ManufacturingFacilityUsageResult(0, 0, 0, 0, 0),
                IncludeComponentManufacturingUsage: false,
                ComponentFacilityUsage: 0,
                IncludeCapitalComponentManufacturingUsage: false,
                CapitalComponentFacilityUsage: 0,
                IncludeReactionUsage: false,
                TotalReactionFacilityUsage: 0,
                HasReprocessingFacility: false,
                IncludeReprocessingUsage: false,
                ReprocessingUsage: 0),
            ItemSaleCharges: new ManufacturingSaleAdjustmentInput(true, 100_000, false, 0, 0, SalesBrokerFeeMode.SpecialFee, 0, 0, 0, 0, 0.02, 0),
            SellExcessAdjustment: new ManufacturingSaleAdjustmentInput(false, 0, false, 0, 0, SalesBrokerFeeMode.NoFee, 0, 0, 0, 0, 0, 0),
            BuildBuy: new ManufacturingBuildBuyInput("Widget", 100, 1, 1, 50, 0, false, false, false, false, false, false, false),
            Activity: new ManufacturingActivityInput(
                IsTech3: true,
                IncludeCopyUsage: false,
                IncludeInventionUsage: true,
                IncludeCopyTime: false,
                IncludeInventionTime: true,
                TotalInventedRuns: 30,
                NumberOfInventionJobs: 6,
                NumberOfInventionSessions: 3,
                UserCopyRuns: 0,
                EstimatedItemValue: 1_000_000,
                CopyCostIndex: 0.05,
                CopyFwCostBonus: 1,
                CopyFacilityCostMultiplier: 1,
                CopyFacilityTaxRate: 0.05,
                InventionCostIndex: 0.03,
                InventionFwCostBonus: 1,
                InventionFacilityCostMultiplier: 1,
                InventionFacilityTaxRate: 0.05,
                BaseCopyTimeSeconds: 1_000,
                BaseInventionTimeSeconds: 9_000,
                ScienceSkillLevel: 4,
                AdvancedIndustrySkillLevel: 4,
                CopyFacilityTimeMultiplier: 1,
                InventionFacilityTimeMultiplier: 0.7,
                CopyImplantBonus: 0.01),
            ComponentSchedule: new ComponentProductionScheduleInput([300d, 120d], 4),
            BaseBlueprintProductionTimeSeconds: 7_200,
            IsTech3: true,
            IncludeInventionCosts: true,
            IncludeCopyCosts: true,
            UserRuns: 10,
            TotalInventedRuns: 30,
            PerInventionRunCost: 500,
            TotalCopyCost: 9_000,
            ItemMarketCost: 100_000,
            RawMaterialsCost: 20_000,
            ComponentMaterialsCost: 30_000,
            AdditionalCosts: 1_000,
            IsBuildBuy: true);

        var result = _sut.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Prerequisites.CanBuild.Should().BeFalse();
        result.Value.BuildBuy.BuildItem.Should().BeFalse();
        result.Value.Cost.CopyCost.Should().Be(0);
        result.Value.Profitability.TotalIskPerHourComponent.Should().BeApproximately(result.Value.Profitability.TotalIskPerHourRaw, 0.000001d);
    }
}