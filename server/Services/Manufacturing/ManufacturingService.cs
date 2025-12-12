using EveIph.Server.Services.Market;
using server.Models;
using server.Services.Blueprints;

namespace server.Services.Manufacturing;

public sealed class ManufacturingService : IManufacturingService
{
    private readonly IBlueprintService _blueprints;
    private readonly IMarketPriceService _prices;

    public ManufacturingService(IBlueprintService blueprints, IMarketPriceService prices)
    {
        _blueprints = blueprints;
        _prices = prices;
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
                0m,
                0m,
                0m,
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
                0m,
                0m,
                0m,
                warnings);
        }

        var totalUnits = Math.Max(1, request.TotalUnits);
        var me = Math.Clamp(request.MaterialEfficiency, 0, 10);

        var typeIds = new HashSet<int>();
        typeIds.Add((int)mfgActivity.ProductId);
        foreach (var mat in mfgActivity.Materials)
        {
            typeIds.Add((int)mat.MaterialId);
        }

        var priceMap = await _prices.GetCachedPricesAsync(typeIds, request.RegionId);

        var componentItems = new List<ManufacturingLineItem>();
        foreach (var mat in mfgActivity.Materials)
        {
            var baseQty = Math.Max(0, mat.Quantity);
            var adjustedQty = MaterialMath.CalculateAdjustedQuantity(baseQty, me, totalUnits);

            var hasPrice = priceMap.TryGetValue((int)mat.MaterialId, out var mp);
            var unitPrice = hasPrice ? mp!.SellPrice : 0m;
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

        var hasProductPrice = priceMap.TryGetValue((int)mfgActivity.ProductId, out var productPrice);
        var productUnitPrice = hasProductPrice ? productPrice!.SellPrice : 0m;
        if (!hasProductPrice)
        {
            warnings.Add($"Missing cached price for product '{mfgActivity.ProductName}' ({mfgActivity.ProductId}).");
        }

        var productValue = productUnitPrice * (mfgActivity.ProductQuantity * totalUnits);
        var profit = productValue - componentTotal;

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
                var rawUnitPrice = hasRawPrice ? rmPrice!.SellPrice : 0m;
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

        // IPH: placeholder until time calculation is implemented.
        var iph = 0m;
        if (warnings.Count == 0)
        {
            // Keep deterministic output, even when no warnings.
        }

        return new ManufacturingResponse(
            details.BlueprintId,
            details.BlueprintName,
            request.RegionId,
            me,
            request.TimeEfficiency,
            totalUnits,
            componentItems,
            rawItems,
            componentTotal,
            rawTotal,
            productValue,
            profit,
            iph,
            warnings);
    }
}

