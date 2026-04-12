using EVE.IPH.Domain.Core.Enumerations;
using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Models;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Manufacturing.Models;
using EVE.IPH.Domain.Manufacturing.Services;
using EVE.IPH.Infrastructure.Settings.Models;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class ManufacturingWorkspaceCommandService(
    IBlueprintRepository blueprintRepository,
    IItemRepository itemRepository,
    ICharacterSkillRepository characterSkillRepository,
    IManufacturingFacilityConfigurationService manufacturingFacilityConfigurationService,
    ManufacturingAnalysisService manufacturingAnalysisService,
    ApplicationSettingsModel? applicationSettings = null) : IManufacturingWorkspaceCommandService
{
    private const string AdvancedIndustrySkillName = "Advanced Industry";
    private const string ScienceSkillName = "Science";
    private const string AccountingSkillName = "Accounting";
    private const string BrokerRelationsSkillName = "Broker Relations";

    private readonly IBlueprintRepository _blueprintRepository = blueprintRepository ?? throw new ArgumentNullException(nameof(blueprintRepository));
    private readonly IItemRepository _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
    private readonly ICharacterSkillRepository _characterSkillRepository = characterSkillRepository ?? throw new ArgumentNullException(nameof(characterSkillRepository));
    private readonly IManufacturingFacilityConfigurationService _manufacturingFacilityConfigurationService = manufacturingFacilityConfigurationService ?? throw new ArgumentNullException(nameof(manufacturingFacilityConfigurationService));
    private readonly ManufacturingAnalysisService _manufacturingAnalysisService = manufacturingAnalysisService ?? throw new ArgumentNullException(nameof(manufacturingAnalysisService));
    private readonly ApplicationSettingsModel _applicationSettings = applicationSettings ?? new ApplicationSettingsModel();

    public async Task<Result<ManufacturingWorkspaceAnalysisResult>> AnalyzeAsync(
        ManufacturingWorkspaceAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserRuns <= 0)
        {
            return Result<ManufacturingWorkspaceAnalysisResult>.Failure("INVALID_USER_RUNS", "User runs must be greater than zero.");
        }

        Maybe<BlueprintRecord> blueprint = await _blueprintRepository.GetBlueprintAsync(request.BlueprintId, cancellationToken).ConfigureAwait(false);
        if (blueprint.HasNoValue)
        {
            return Result<ManufacturingWorkspaceAnalysisResult>.Failure("BLUEPRINT_NOT_FOUND", $"Blueprint {request.BlueprintId.Value} was not found in the SDE.");
        }

        Maybe<ItemRecord> product = await _itemRepository.GetItemAsync(blueprint.Value.ProductTypeId, cancellationToken).ConfigureAwait(false);

        Result<IReadOnlyList<SkillRequirement>> requiredSkillsResult = await _blueprintRepository
            .GetRequiredSkillsAsync(request.BlueprintId, ActivityType.Manufacturing, cancellationToken)
            .ConfigureAwait(false);
        if (requiredSkillsResult.IsFailure)
        {
            return Result<ManufacturingWorkspaceAnalysisResult>.Failure(requiredSkillsResult.Error);
        }

        Result<IReadOnlyList<CharacterSkillRecord>> characterSkillsResult = await _characterSkillRepository
            .GetByCharacterIdAsync(request.FacilityCharacterId, cancellationToken)
            .ConfigureAwait(false);
        if (characterSkillsResult.IsFailure)
        {
            return Result<ManufacturingWorkspaceAnalysisResult>.Failure(characterSkillsResult.Error);
        }

        Result<Maybe<ResolvedIndustryFacilityConfiguration>> facilityResult = await _manufacturingFacilityConfigurationService
            .GetFacilityAsync(request.FacilityCharacterId, request.ProductionType, cancellationToken)
            .ConfigureAwait(false);
        if (facilityResult.IsFailure)
        {
            return Result<ManufacturingWorkspaceAnalysisResult>.Failure(facilityResult.Error);
        }

        if (facilityResult.Value.HasNoValue || facilityResult.Value.Value.Configuration.FacilityId != request.FacilityId)
        {
            return Result<ManufacturingWorkspaceAnalysisResult>.Failure("FACILITY_NOT_FOUND", "The selected manufacturing facility is no longer available.");
        }

        ResolvedIndustryFacilityConfiguration facility = facilityResult.Value.Value;
        IReadOnlyList<CharacterSkillRecord> skills = characterSkillsResult.Value;
        Dictionary<long, int> skillLevels = skills.ToDictionary(skill => skill.SkillTypeId.Value, ResolveSkillLevel);

        int scienceLevel = GetSkillLevelByName(skills, ScienceSkillName);
        int advancedIndustryLevel = GetSkillLevelByName(skills, AdvancedIndustrySkillName);
        int accountingLevel = GetSkillLevelByName(skills, AccountingSkillName);
        int brokerRelationsLevel = GetSkillLevelByName(skills, BrokerRelationsSkillName);

        TechLevel techLevel = blueprint.Value.TechLevel;
        bool includeResearchActivities = techLevel is TechLevel.T2 or TechLevel.T3;
        bool isTech3 = techLevel == TechLevel.T3;
        double estimatedItemValue = request.EstimatedItemValue > 0 ? request.EstimatedItemValue : request.ItemMarketCost;
        long totalInventedRuns = includeResearchActivities ? Math.Max(request.UserRuns, 1) : 0;
        int numberOfInventionJobs = includeResearchActivities ? 1 : 0;
        int numberOfInventionSessions = includeResearchActivities ? 1 : 0;
        int userCopyRuns = includeResearchActivities ? 1 : 0;
        double costMultiplier = facility.Configuration.CostMultiplierOverride.GetValueOrDefault(1d);
        double timeMultiplier = facility.Configuration.TimeMultiplierOverride.GetValueOrDefault(1d);
        double fwCostBonus = GetFactionWarfareCostBonus(facility.Configuration.FactionWarfareUpgradeLevel);
        long portionSize = product.HasValue ? Math.Max(product.Value.PortionSize, 1) : 1;

        ManufacturingAnalysisInput input = new(
            Prerequisites: new ManufacturingPrerequisiteInput(
                requiredSkillsResult.Value.Select(skill => new ManufacturingSkillRequirement(skill.SkillTypeId.Value, skill.Level)).ToArray(),
                skillLevels),
            FacilityUsage: new ManufacturingFacilityUsageInput(
                IncludeManufacturingUsage: true,
                EstimatedItemValue: estimatedItemValue,
                UserRuns: request.UserRuns,
                CostIndex: facility.Configuration.CostIndex,
                CostMultiplier: costMultiplier,
                FwManufacturingCostBonus: fwCostBonus,
                FacilityTaxRate: facility.Configuration.TaxRate,
                SccIndustryFeeSurcharge: _applicationSettings.SccIndustryFeeSurcharge,
                IsAlphaAccount: _applicationSettings.AlphaAccount,
                AlphaAccountTaxRate: _applicationSettings.AlphaAccountTaxRate,
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
            ItemSaleCharges: new ManufacturingSaleAdjustmentInput(
                IncludeSale: true,
                GrossSaleAmount: request.ItemMarketCost,
                ApplySalesTax: request.ApplySalesTax,
                BaseSalesTaxRatePercent: _applicationSettings.BaseSalesTaxRate,
                AccountingSkillLevel: accountingLevel,
                BrokerFeeMode: request.IncludeBrokerFee ? SalesBrokerFeeMode.Fee : SalesBrokerFeeMode.NoFee,
                BaseBrokerFeeRatePercent: _applicationSettings.BaseBrokerFeeRate,
                BrokerRelationsSkillLevel: brokerRelationsLevel,
                BrokerFactionStanding: _applicationSettings.BrokerFactionStanding,
                BrokerCorporationStanding: _applicationSettings.BrokerCorpStanding,
                FixedBrokerFeeRate: 0,
                SccBrokerFeeSurcharge: _applicationSettings.SccBrokerFeeSurcharge),
            SellExcessAdjustment: new ManufacturingSaleAdjustmentInput(
                IncludeSale: false,
                GrossSaleAmount: 0,
                ApplySalesTax: false,
                BaseSalesTaxRatePercent: 0,
                AccountingSkillLevel: 0,
                BrokerFeeMode: SalesBrokerFeeMode.NoFee,
                BaseBrokerFeeRatePercent: 0,
                BrokerRelationsSkillLevel: 0,
                BrokerFactionStanding: 0,
                BrokerCorporationStanding: 0,
                FixedBrokerFeeRate: 0,
                SccBrokerFeeSurcharge: 0),
            BuildBuy: new ManufacturingBuildBuyInput(
                blueprint.Value.ProductName,
                request.ItemMarketCost,
                portionSize,
                request.UserRuns,
                request.RawMaterialsCost + request.ComponentMaterialsCost + request.AdditionalCosts,
                0,
                OwnedBlueprint: true,
                IsNewBlueprintRequest: false,
                SuggestBuildWhenBlueprintNotOwned: _applicationSettings.SuggestBuildBpNotOwned,
                AlwaysBuyFuelBlocks: _applicationSettings.AlwaysBuyFuelBlocks,
                AlwaysBuyRams: _applicationSettings.AlwaysBuyRams,
                ForceBuildBecauseMarketInsufficient: _applicationSettings.BuildWhenNotEnoughItemsOnMarket,
                ManualBuildOverride: null),
            Activity: new ManufacturingActivityInput(
                IsTech3: isTech3,
                IncludeCopyUsage: includeResearchActivities,
                IncludeInventionUsage: includeResearchActivities,
                IncludeCopyTime: includeResearchActivities,
                IncludeInventionTime: includeResearchActivities,
                TotalInventedRuns: totalInventedRuns,
                NumberOfInventionJobs: numberOfInventionJobs,
                NumberOfInventionSessions: numberOfInventionSessions,
                UserCopyRuns: userCopyRuns,
                EstimatedItemValue: estimatedItemValue,
                CopyCostIndex: facility.Configuration.CostIndex,
                CopyFwCostBonus: fwCostBonus,
                CopyFacilityCostMultiplier: costMultiplier,
                CopyFacilityTaxRate: facility.Configuration.TaxRate,
                InventionCostIndex: facility.Configuration.CostIndex,
                InventionFwCostBonus: fwCostBonus,
                InventionFacilityCostMultiplier: costMultiplier,
                InventionFacilityTaxRate: facility.Configuration.TaxRate,
                BaseCopyTimeSeconds: blueprint.Value.CopyTime,
                BaseInventionTimeSeconds: blueprint.Value.InventionTime,
                ScienceSkillLevel: scienceLevel,
                AdvancedIndustrySkillLevel: advancedIndustryLevel,
                CopyFacilityTimeMultiplier: timeMultiplier,
                InventionFacilityTimeMultiplier: timeMultiplier,
                CopyImplantBonus: _applicationSettings.CopyImplantValue),
            ComponentSchedule: new ComponentProductionScheduleInput([], 1),
            BaseBlueprintProductionTimeSeconds: blueprint.Value.ManufacturingTime,
            IsTech3: isTech3,
            IncludeInventionCosts: includeResearchActivities,
            IncludeCopyCosts: includeResearchActivities,
            UserRuns: request.UserRuns,
            TotalInventedRuns: totalInventedRuns,
            PerInventionRunCost: includeResearchActivities && totalInventedRuns > 0 ? request.AdditionalCosts / totalInventedRuns : 0,
            TotalCopyCost: 0,
            ItemMarketCost: request.ItemMarketCost,
            RawMaterialsCost: request.RawMaterialsCost,
            ComponentMaterialsCost: request.ComponentMaterialsCost,
            AdditionalCosts: request.AdditionalCosts,
            IsBuildBuy: false);

        Result<ManufacturingAnalysisResult> result = _manufacturingAnalysisService.Calculate(input);
        if (result.IsFailure)
        {
            return Result<ManufacturingWorkspaceAnalysisResult>.Failure(result.Error);
        }

        string productName = product.HasValue ? product.Value.TypeName : blueprint.Value.ProductName;

        return Result<ManufacturingWorkspaceAnalysisResult>.Success(new ManufacturingWorkspaceAnalysisResult(
            blueprint.Value.BlueprintId.Value.ToString(),
            productName,
            facility.Configuration.FacilityName,
            facility.Configuration.CharacterId.Value.ToString(),
            result.Value.Prerequisites.CanBuild,
            result.Value.BuildBuy.CheaperToBuild,
            result.Value.BuildBuy.BuildItem,
            request.UserRuns,
            result.Value.Profitability.TotalRawCost,
            result.Value.Profitability.TotalComponentCost,
            result.Value.Profitability.TotalRawProfit,
            result.Value.Profitability.TotalComponentProfit,
            result.Value.Profitability.TotalIskPerHourRaw,
            result.Value.Profitability.TotalIskPerHourComponent,
            result.Value.Cost.TotalUsage,
            result.Value.Timeline.TotalProductionTimeSeconds,
            result.Value.Timeline.BlueprintProductionTimeSeconds,
            $"Calculated a manufacturing snapshot for {productName} using {facility.Configuration.FacilityName}."));
    }

    private static int ResolveSkillLevel(CharacterSkillRecord skill) => skill.IsOverridden ? skill.OverrideLevel : skill.ActiveLevel;

    private static int GetSkillLevelByName(IReadOnlyList<CharacterSkillRecord> skills, string skillName)
    {
        CharacterSkillRecord? skill = skills.FirstOrDefault(candidate => string.Equals(candidate.Name, skillName, StringComparison.OrdinalIgnoreCase));
        return skill is null ? 0 : ResolveSkillLevel(skill);
    }

    private static double GetFactionWarfareCostBonus(int upgradeLevel) => upgradeLevel switch
    {
        1 => 0.9d,
        2 => 0.8d,
        3 => 0.7d,
        4 => 0.6d,
        5 => 0.5d,
        _ => 1d,
    };
}