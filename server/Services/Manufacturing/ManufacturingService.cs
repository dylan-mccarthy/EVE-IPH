using EveIph.Server.Services.Market;
using MarketPrice = EveIph.Server.Models.MarketPrice;
using server.Models;
using server.Services.Blueprints;
using server.Services.IndustryCosts;

namespace server.Services.Manufacturing;

public sealed class ManufacturingService : IManufacturingService
{
    private readonly IBlueprintService _blueprints;
    private readonly IMarketPriceService _prices;
    private readonly IIndustryCostService _industryCosts;

    public ManufacturingService(IBlueprintService blueprints, IMarketPriceService prices, IIndustryCostService industryCosts)
    {
        _blueprints = blueprints;
        _prices = prices;
        _industryCosts = industryCosts;
    }

    public async Task<ManufacturingResponse> CalculateAsync(ManufacturingRequest request, CancellationToken ct = default)
    {
        var warnings = new List<string>();

        var details = await _blueprints.GetDetailsAsync(request.BlueprintId, ct);
        if (details is null)
        {
            warnings.Add("Blueprint not found.");
            return new ManufacturingResponse(
                request.BlueprintId,
                "Unknown",
                request.RegionId,
                request.MaterialEfficiency,
                request.TimeEfficiency,
                request.TotalUnits,
                Array.Empty<ManufacturingLineItem>(),
                Array.Empty<ManufacturingLineItem>(),
                0m,
                0m,
                null,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                null,
                0m,
                0m,
                0m,
                null,
                0m,
                null,
                warnings);
        }

        var mfgActivity = details.Activities.FirstOrDefault(a => a.ActivityName == "Manufacturing");
        if (mfgActivity is null)
        {
            warnings.Add("Blueprint has no Manufacturing activity.");
            return new ManufacturingResponse(
                details.BlueprintId,
                details.BlueprintName,
                request.RegionId,
                request.MaterialEfficiency,
                request.TimeEfficiency,
                request.TotalUnits,
                Array.Empty<ManufacturingLineItem>(),
                Array.Empty<ManufacturingLineItem>(),
                0m,
                0m,
                null,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                null,
                0m,
                0m,
                0m,
                null,
                0m,
                null,
                warnings);
        }

        var totalUnits = Math.Max(1, request.TotalUnits);
        var me = Math.Clamp(request.MaterialEfficiency, 0, 10);
        var te = Math.Clamp(request.TimeEfficiency, 0, 20);
        var productionLines = Math.Max(1, request.ProductionLines);

        var facilityMaterialMultiplier = request.FacilityMaterialMultiplier;
        if (facilityMaterialMultiplier < 0m) facilityMaterialMultiplier = 0m;
        if (facilityMaterialMultiplier > 10m) facilityMaterialMultiplier = 10m;

        var facilityTimeMultiplier = request.FacilityTimeMultiplier;
        if (facilityTimeMultiplier < 0m) facilityTimeMultiplier = 0m;
        if (facilityTimeMultiplier > 10m) facilityTimeMultiplier = 10m;

        var systemCostIndex = request.SystemCostIndex;
        if (systemCostIndex < 0m) systemCostIndex = 0m;
        if (systemCostIndex > 1m) systemCostIndex = 1m;

        var facilityCostMultiplier = request.FacilityCostMultiplier;
        if (facilityCostMultiplier < 0m) facilityCostMultiplier = 0m;
        if (facilityCostMultiplier > 10m) facilityCostMultiplier = 10m;

        var facilityTaxRate = request.FacilityTaxRate;
        if (facilityTaxRate < 0m) facilityTaxRate = 0m;
        if (facilityTaxRate > 1m) facilityTaxRate = 1m;

        var sccSurchargeRate = request.SccSurchargeRate;
        if (sccSurchargeRate < 0m) sccSurchargeRate = 0m;
        if (sccSurchargeRate > 1m) sccSurchargeRate = 1m;

        var useIndustryCostModel = request.JobInstallationCost <= 0m
            && (systemCostIndex != 0m || facilityTaxRate != 0m || sccSurchargeRate != 0m || facilityCostMultiplier != 1m);

        var materialMarketMode = string.IsNullOrWhiteSpace(request.MaterialMarketMode)
            ? "Buy"
            : request.MaterialMarketMode.Trim();

        var salesTaxRate = request.SalesTaxRate;
        if (salesTaxRate < 0m) salesTaxRate = 0m;
        if (salesTaxRate > 1m) salesTaxRate = 1m;

        var brokerFeeRate = request.BrokerFeeRate;
        if (brokerFeeRate < 0m) brokerFeeRate = 0m;
        if (brokerFeeRate > 1m) brokerFeeRate = 1m;

        // Legacy-aligned meanings (Blueprint.vb comments, input-side):
        // - Buy: buy from sell order (min sell), no tax/broker
        // - Buy Order: buy via buy order (max buy), broker only
        var matModeIsBuy = materialMarketMode.Equals("Buy", StringComparison.OrdinalIgnoreCase);
        var matModeIsBuyOrder = materialMarketMode.Equals("BuyOrder", StringComparison.OrdinalIgnoreCase)
            || materialMarketMode.Equals("Buy Order", StringComparison.OrdinalIgnoreCase);

        if (!(matModeIsBuy || matModeIsBuyOrder))
        {
            warnings.Add($"Unknown MaterialMarketMode '{request.MaterialMarketMode}', defaulting to Buy.");
            matModeIsBuy = true;
            matModeIsBuyOrder = false;
        }

        var typeIds = new HashSet<int>();
        typeIds.Add((int)mfgActivity.ProductId);
        foreach (var mat in mfgActivity.Materials)
        {
            typeIds.Add((int)mat.MaterialId);
        }

        var priceMap = await _prices.GetCachedPricesAsync(typeIds, request.RegionId);

        Dictionary<int, decimal>? adjustedPriceMap = null;
        if (useIndustryCostModel)
        {
            var materialTypeIds = mfgActivity.Materials.Select(m => (int)m.MaterialId).Distinct().ToList();
            adjustedPriceMap = await _industryCosts.GetAdjustedPricesAsync(materialTypeIds, ct);
        }

        var componentItems = new List<ManufacturingLineItem>();
        decimal totalEiv = 0m; // Estimated Item Value across all runs (adjusted-price based)
        foreach (var mat in mfgActivity.Materials)
        {
            var baseQty = Math.Max(0, mat.Quantity);
            var adjustedQty = MaterialMath.CalculateAdjustedQuantity(baseQty, me, totalUnits, facilityMaterialMultiplier);

            if (useIndustryCostModel && adjustedPriceMap != null)
            {
                if (adjustedPriceMap.TryGetValue((int)mat.MaterialId, out var adjustedPrice))
                {
                    totalEiv += adjustedPrice * adjustedQty;
                }
                else
                {
                    // Missing adjusted prices degrade installation cost accuracy but shouldn't fail the calc.
                    warnings.Add($"Missing adjusted price for material '{mat.MaterialName}' ({mat.MaterialId}).");
                }
            }

            var hasPrice = priceMap.TryGetValue((int)mat.MaterialId, out var mp);
            var unitPrice = 0m;
            if (hasPrice)
            {
                unitPrice = matModeIsBuyOrder ? mp!.BuyPrice : mp!.SellPrice;

                if (matModeIsBuyOrder && unitPrice <= 0m && mp!.SellPrice > 0m)
                {
                    warnings.Add($"Material market mode '{materialMarketMode}' uses buyPrice, but buyPrice is missing/0 for material '{mat.MaterialName}' ({mat.MaterialId}).");
                }

                // Broker fee applies up-front for buy orders (no tax for buying).
                if (matModeIsBuyOrder && brokerFeeRate > 0m)
                {
                    unitPrice = unitPrice * (1m + brokerFeeRate);
                }
            }
            var totalCost = unitPrice * adjustedQty;

            if (!hasPrice)
            {
                warnings.Add($"Missing cached price for material '{mat.MaterialName}' ({mat.MaterialId}).");
            }

            componentItems.Add(new ManufacturingLineItem(
                (int)mat.MaterialId,
                mat.MaterialName,
                adjustedQty,
                unitPrice,
                totalCost,
                MissingPrice: !hasPrice));
        }

        var componentTotal = componentItems.Sum(i => i.TotalCost);

        var productMarketMode = string.IsNullOrWhiteSpace(request.ProductMarketMode)
            ? "SellOrder"
            : request.ProductMarketMode.Trim();

        // Legacy-aligned meanings (Blueprint.vb comments):
        // - Buy: buy from sell order (min sell), no tax/broker
        // - Sell Order: sell via sell order (min sell), tax + broker
        // - Buy Order: buy via buy order (max buy), broker only
        // - Sell: sell to buy order (max buy), tax only
        var modeIsBuy = productMarketMode.Equals("Buy", StringComparison.OrdinalIgnoreCase);
        var modeIsSellOrder = productMarketMode.Equals("SellOrder", StringComparison.OrdinalIgnoreCase)
            || productMarketMode.Equals("Sell Order", StringComparison.OrdinalIgnoreCase);
        var modeIsBuyOrder = productMarketMode.Equals("BuyOrder", StringComparison.OrdinalIgnoreCase)
            || productMarketMode.Equals("Buy Order", StringComparison.OrdinalIgnoreCase);
        var modeIsSell = productMarketMode.Equals("Sell", StringComparison.OrdinalIgnoreCase);

        if (!(modeIsBuy || modeIsSellOrder || modeIsBuyOrder || modeIsSell))
        {
            warnings.Add($"Unknown ProductMarketMode '{request.ProductMarketMode}', defaulting to SellOrder.");
            modeIsSellOrder = true;
            modeIsBuy = false;
            modeIsBuyOrder = false;
            modeIsSell = false;
        }

        var includeTax = modeIsSellOrder || modeIsSell;
        var includeBroker = modeIsSellOrder || modeIsBuyOrder;

        var hasProductPrice = priceMap.TryGetValue((int)mfgActivity.ProductId, out var productPrice);
        var productUnitPrice = 0m;
        if (hasProductPrice)
        {
            var useMaxBuy = modeIsBuyOrder || modeIsSell;
            productUnitPrice = useMaxBuy ? productPrice!.BuyPrice : productPrice!.SellPrice;

            if (useMaxBuy && productUnitPrice <= 0m && productPrice!.SellPrice > 0m)
            {
                warnings.Add($"Product market mode '{productMarketMode}' uses buyPrice, but buyPrice is missing/0 for '{mfgActivity.ProductName}' ({mfgActivity.ProductId}).");
            }
        }
        if (!hasProductPrice)
        {
            warnings.Add($"Missing cached price for product '{mfgActivity.ProductName}' ({mfgActivity.ProductId}).");
        }

        var productValue = productUnitPrice * (mfgActivity.ProductQuantity * totalUnits);

        var jobCost = request.JobInstallationCost;
        if (jobCost < 0m) jobCost = 0m;

        if (jobCost <= 0m && useIndustryCostModel)
        {
            // Legacy shape (manufacturing):
            // totalInstallationCost = TotalEIV * (systemCostIndex * costMultiplier + facilityTax + sccSurcharge [+ alpha])
            var indexBonuses = systemCostIndex * facilityCostMultiplier;
            jobCost = totalEiv * (indexBonuses + facilityTaxRate + sccSurchargeRate);
        }

        var salesTax = productValue * (includeTax ? salesTaxRate : 0m);
        var brokerFee = productValue * (includeBroker ? brokerFeeRate : 0m);

        // Raw materials: reuse recursive breakdown and then price from cached market data.
        var rawResponse = await _blueprints.GetRawMaterialsAsync(
            new RawMaterialsRequest(request.BlueprintId, MaterialEfficiency: me, Runs: totalUnits),
            ct);

        IReadOnlyList<ManufacturingLineItem> rawItems;
        decimal rawTotal;
        if (rawResponse is null)
        {
            rawItems = Array.Empty<ManufacturingLineItem>();
            rawTotal = 0m;
        }
        else
        {
            var rawTypeIds = rawResponse.RawMaterials.Select(r => r.TypeId).Distinct().ToList();
            var rawPriceMap = await _prices.GetCachedPricesAsync(rawTypeIds, request.RegionId);

            var tmp = new List<ManufacturingLineItem>();
            foreach (var rm in rawResponse.RawMaterials)
            {
                var hasRawPrice = rawPriceMap.TryGetValue(rm.TypeId, out var rmPrice);
                var rawUnitPrice = 0m;
                if (hasRawPrice)
                {
                    rawUnitPrice = matModeIsBuyOrder ? rmPrice!.BuyPrice : rmPrice!.SellPrice;

                    if (matModeIsBuyOrder && rawUnitPrice <= 0m && rmPrice!.SellPrice > 0m)
                    {
                        warnings.Add($"Material market mode '{materialMarketMode}' uses buyPrice, but buyPrice is missing/0 for raw material '{rm.TypeName}' ({rm.TypeId}).");
                    }

                    if (matModeIsBuyOrder && brokerFeeRate > 0m)
                    {
                        rawUnitPrice = rawUnitPrice * (1m + brokerFeeRate);
                    }
                }
                var rawTotalCost = rawUnitPrice * rm.Quantity;
                if (!hasRawPrice)
                {
                    warnings.Add($"Missing cached price for raw material '{rm.TypeName}' ({rm.TypeId}).");
                }

                tmp.Add(new ManufacturingLineItem(
                    rm.TypeId,
                    rm.TypeName,
                    rm.Quantity,
                    rawUnitPrice,
                    rawTotalCost,
                    MissingPrice: !hasRawPrice));
            }

            rawItems = tmp;
            rawTotal = tmp.Sum(i => i.TotalCost);
        }

        // Time + IPH
        // BaseProductionTimeSeconds is for one manufacturing run.
        var baseProductionTimeSeconds = details.BaseProductionTimeSeconds.GetValueOrDefault(0);
        if (baseProductionTimeSeconds <= 0)
        {
            warnings.Add("Missing base production time for blueprint (BASE_PRODUCTION_TIME).");
        }

        var teModifier = 1m - (te / 100m);
        if (teModifier < 0m) teModifier = 0m;

        var totalSeconds = (decimal)baseProductionTimeSeconds * totalUnits * teModifier * facilityTimeMultiplier;
        var wallSeconds = productionLines <= 1 ? totalSeconds : (totalSeconds / productionLines);
        var totalHours = wallSeconds / 3600m;

        var profitCostBasis = string.IsNullOrWhiteSpace(request.ProfitCostBasis)
            ? "Components"
            : request.ProfitCostBasis.Trim();

        var useRawForProfit = profitCostBasis.Equals("Raw", StringComparison.OrdinalIgnoreCase)
            || profitCostBasis.Equals("RawMaterials", StringComparison.OrdinalIgnoreCase)
            || profitCostBasis.Equals("Raw Materials", StringComparison.OrdinalIgnoreCase);

        var useBuildBuyForProfit = profitCostBasis.Equals("BuildBuy", StringComparison.OrdinalIgnoreCase)
            || profitCostBasis.Equals("Build/Buy", StringComparison.OrdinalIgnoreCase)
            || profitCostBasis.Equals("Build Buy", StringComparison.OrdinalIgnoreCase);

        decimal? buildBuyTotalCost = null;
        decimal? excessSellValueNet = null;
        if (useBuildBuyForProfit)
        {
            var totals = await CalculateBuildBuyTotalsAsync(
                requiredMaterials: componentItems,
                me,
                facilityMaterialMultiplier,
                matModeIsBuyOrder,
                brokerFeeRate,
                request.RegionId,
                request.SellExcessItems,
                salesTaxRate,
                brokerFeeRate,
                warnings,
                ct);

            buildBuyTotalCost = totals.TotalCost;
            excessSellValueNet = totals.ExcessSellValueNet;
        }

        var profitComponents = productValue - componentTotal - salesTax - brokerFee - jobCost;
        var profitRaw = productValue - rawTotal - salesTax - brokerFee - jobCost;

        decimal? profitBuildBuy = null;
        if (buildBuyTotalCost is not null)
        {
            profitBuildBuy = productValue - buildBuyTotalCost.Value - salesTax - brokerFee - jobCost;
        }

        var iphComponents = totalHours > 0m ? (profitComponents / totalHours) : 0m;
        var iphRaw = totalHours > 0m ? (profitRaw / totalHours) : 0m;

        decimal? iphBuildBuy = null;
        if (profitBuildBuy is not null)
        {
            iphBuildBuy = totalHours > 0m ? (profitBuildBuy.Value / totalHours) : 0m;
        }

        if (!(useRawForProfit || useBuildBuyForProfit || profitCostBasis.Equals("Components", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add($"Unknown ProfitCostBasis '{request.ProfitCostBasis}', defaulting to Components.");
        }

        var profit = useBuildBuyForProfit ? (profitBuildBuy ?? profitComponents) : (useRawForProfit ? profitRaw : profitComponents);
        var iph = useBuildBuyForProfit ? (iphBuildBuy ?? iphComponents) : (useRawForProfit ? iphRaw : iphComponents);

        return new ManufacturingResponse(
            details.BlueprintId,
            details.BlueprintName,
            request.RegionId,
            me,
            te,
            totalUnits,
            componentItems,
            rawItems,
            componentTotal,
            rawTotal,
            buildBuyTotalCost,
            productValue,
            salesTax,
            brokerFee,
            jobCost,
            totalEiv,
            wallSeconds,
            profitComponents,
            profitRaw,
            profitBuildBuy,
            profit,
            iphComponents,
            iphRaw,
            iphBuildBuy,
            iph,
            excessSellValueNet,
            warnings);
    }

    private sealed record BuildBuyTotals(decimal TotalCost, decimal ExcessSellValueNet);

    private async Task<BuildBuyTotals> CalculateBuildBuyTotalsAsync(
        IReadOnlyList<ManufacturingLineItem> requiredMaterials,
        int me,
        decimal facilityMaterialMultiplier,
        bool matModeIsBuyOrder,
        decimal brokerFeeRate,
        int regionId,
        bool sellExcessItems,
        decimal salesTaxRate,
        decimal salesBrokerFeeRate,
        List<string> warnings,
        CancellationToken ct)
    {
        var blueprintIdByProduct = new Dictionary<long, long?>();
        var blueprintDetailsCache = new Dictionary<long, BlueprintDetails?>();
        var priceCache = new Dictionary<int, MarketPrice>();

        async Task EnsurePricesAsync(IEnumerable<int> typeIds)
        {
            var missing = typeIds.Where(id => !priceCache.ContainsKey(id)).Distinct().ToList();
            if (missing.Count == 0) return;

            var fetched = await _prices.GetCachedPricesAsync(missing, regionId);
            foreach (var kvp in fetched)
            {
                if (kvp.Value is not null)
                {
                    priceCache[kvp.Key] = kvp.Value;
                }
            }
        }

        async Task<MarketPrice?> GetPriceAsync(int typeId)
        {
            if (priceCache.TryGetValue(typeId, out var mp)) return mp;
            await EnsurePricesAsync(new[] { typeId });
            return priceCache.TryGetValue(typeId, out mp) ? mp : null;
        }

        async Task<long?> GetManufacturingBlueprintIdAsync(long productTypeId)
        {
            if (blueprintIdByProduct.TryGetValue(productTypeId, out var cached)) return cached;
            var found = await _blueprints.FindManufacturingBlueprintIdByProductTypeIdAsync(productTypeId, ct);
            blueprintIdByProduct[productTypeId] = found;
            return found;
        }

        async Task<BlueprintDetails?> GetBlueprintDetailsAsync(long blueprintId)
        {
            if (blueprintDetailsCache.TryGetValue(blueprintId, out var cached)) return cached;
            var details = await _blueprints.GetDetailsAsync(blueprintId, ct);
            blueprintDetailsCache[blueprintId] = details;
            return details;
        }

        decimal ComputeBuyUnitPrice(MarketPrice mp)
        {
            var unit = matModeIsBuyOrder ? mp.BuyPrice : mp.SellPrice;
            if (matModeIsBuyOrder && brokerFeeRate > 0m)
            {
                unit = unit * (1m + brokerFeeRate);
            }
            return unit;
        }

        decimal ComputeExcessNetSellValue(MarketPrice mp, int quantity)
        {
            if (quantity <= 0) return 0m;
            var gross = mp.SellPrice * quantity;
            var net = gross;
            if (salesTaxRate > 0m) net -= gross * salesTaxRate;
            if (salesBrokerFeeRate > 0m) net -= gross * salesBrokerFeeRate;
            return net;
        }

        async Task<(decimal NetCost, decimal ExcessNet)> CalculateBuildCostForTypeAsync(
            long productTypeId,
            int requiredQuantity,
            HashSet<long> visitedBlueprints)
        {
            if (requiredQuantity <= 0) return (0m, 0m);

            var bpId = await GetManufacturingBlueprintIdAsync(productTypeId);
            if (bpId is null || bpId.Value <= 0)
            {
                var mp = await GetPriceAsync((int)productTypeId);
                if (mp is null)
                {
                    warnings.Add($"Missing cached price for build/buy item ({productTypeId}).");
                    return (0m, 0m);
                }

                return (ComputeBuyUnitPrice(mp) * requiredQuantity, 0m);
            }

            if (visitedBlueprints.Contains(bpId.Value))
            {
                var mp = await GetPriceAsync((int)productTypeId);
                if (mp is null)
                {
                    warnings.Add($"Circular dependency and missing cached price for item ({productTypeId}); treating cost as 0.");
                    return (0m, 0m);
                }

                return (ComputeBuyUnitPrice(mp) * requiredQuantity, 0m);
            }

            visitedBlueprints.Add(bpId.Value);

            var details = await GetBlueprintDetailsAsync(bpId.Value);
            var activity = details?.Activities.FirstOrDefault(a => a.ActivityName == "Manufacturing");
            if (details is null || activity is null)
            {
                visitedBlueprints.Remove(bpId.Value);
                var mp = await GetPriceAsync((int)productTypeId);
                if (mp is null)
                {
                    warnings.Add($"Missing blueprint details and price for item ({productTypeId}); treating cost as 0.");
                    return (0m, 0m);
                }

                return (ComputeBuyUnitPrice(mp) * requiredQuantity, 0m);
            }

            var outputQtyPerRun = activity.Products.FirstOrDefault(p => p.ProductId == productTypeId)?.Quantity
                ?? activity.ProductQuantity;
            if (outputQtyPerRun <= 0) outputQtyPerRun = 1;

            var runs = (int)Math.Ceiling(requiredQuantity / (decimal)outputQtyPerRun);
            if (runs < 1) runs = 1;

            var produced = runs * outputQtyPerRun;
            var excessQty = Math.Max(0, produced - requiredQuantity);

            // Preload prices for inputs + product (for excess sellback).
            var ids = new HashSet<int> { (int)productTypeId };
            foreach (var m in activity.Materials)
            {
                ids.Add((int)m.MaterialId);
            }
            await EnsurePricesAsync(ids);

            decimal inputCost = 0m;
            decimal excessNetTotal = 0m;

            foreach (var mat in activity.Materials)
            {
                var baseQty = Math.Max(0, mat.Quantity);
                var adjustedQty = MaterialMath.CalculateAdjustedQuantity(baseQty, me, runs, facilityMaterialMultiplier);
                if (adjustedQty <= 0) continue;

                var mp = await GetPriceAsync((int)mat.MaterialId);
                var buyUnit = mp is null ? 0m : ComputeBuyUnitPrice(mp);
                var buyTotal = buyUnit * adjustedQty;

                var subBpId = await GetManufacturingBlueprintIdAsync(mat.MaterialId);
                if (subBpId is null || subBpId.Value <= 0)
                {
                    if (mp is null)
                    {
                        warnings.Add($"Missing cached price for build/buy input '{mat.MaterialName}' ({mat.MaterialId}).");
                    }
                    inputCost += buyTotal;
                    continue;
                }

                var (buildNetCost, buildExcessNet) = await CalculateBuildCostForTypeAsync(mat.MaterialId, adjustedQty, visitedBlueprints);
                var buildIsCheaper = buyTotal <= 0m || (buildNetCost < buyTotal);
                if (buildIsCheaper)
                {
                    inputCost += buildNetCost;
                    excessNetTotal += buildExcessNet;
                }
                else
                {
                    inputCost += buyTotal;
                }
            }

            if (sellExcessItems && excessQty > 0)
            {
                var mp = await GetPriceAsync((int)productTypeId);
                if (mp is null)
                {
                    warnings.Add($"Missing cached price for excess item ({productTypeId}).");
                }
                else
                {
                    excessNetTotal += ComputeExcessNetSellValue(mp, excessQty);
                }
            }

            visitedBlueprints.Remove(bpId.Value);
            return (inputCost - excessNetTotal, excessNetTotal);
        }

        // Root-level: choose build or buy per direct input.
        await EnsurePricesAsync(requiredMaterials.Select(m => m.TypeId));

        decimal totalCost = 0m;
        decimal totalExcessNet = 0m;
        foreach (var mat in requiredMaterials)
        {
            var buyTotal = mat.TotalCost;

            var bpId = await GetManufacturingBlueprintIdAsync(mat.TypeId);
            if (bpId is null || bpId.Value <= 0)
            {
                if (mat.MissingPrice)
                {
                    warnings.Add($"Missing cached price for build/buy material '{mat.TypeName}' ({mat.TypeId}).");
                }
                totalCost += buyTotal;
                continue;
            }

            var (buildNetCost, buildExcessNet) = await CalculateBuildCostForTypeAsync(mat.TypeId, mat.Quantity, new HashSet<long>());
            var buildIsCheaper = buyTotal <= 0m || mat.MissingPrice || (buildNetCost < buyTotal);
            if (buildIsCheaper)
            {
                totalCost += buildNetCost;
                totalExcessNet += buildExcessNet;
            }
            else
            {
                totalCost += buyTotal;
            }
        }

        return new BuildBuyTotals(totalCost, totalExcessNet);
    }
}

