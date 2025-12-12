using FluentAssertions;
using Moq;
using EveIph.Server.Services.Market;
using server.Models;
using server.Services.Blueprints;
using server.Services.IndustryCosts;
using server.Services.Manufacturing;
using Xunit;

using MarketPrice = EveIph.Server.Models.MarketPrice;

namespace server.Tests.Services.Manufacturing;

public sealed class ManufacturingServiceGoldenTests
{
    [Fact]
    public async Task CalculateAsync_ComputesIph_FromTimeAndProfit()
    {
        var blueprintId = 1L;
        var regionId = 10000002;

        var details = new BlueprintDetails(
            BlueprintId: blueprintId,
            BlueprintName: "Test Blueprint",
            ItemGroup: "Test",
            ItemCategory: "Test",
            BaseProductionTimeSeconds: 600,
            Activities: new List<BlueprintActivity>
            {
                new(
                    ActivityId: 1,
                    ActivityName: "Manufacturing",
                    ProductId: 100,
                    ProductName: "Test Product",
                    ProductQuantity: 1,
                    Materials: new List<BlueprintMaterial>
                    {
                        new(
                            MaterialId: 200,
                            MaterialName: "Test Material",
                            MaterialGroup: "Test",
                            MaterialCategory: "Test",
                            Quantity: 2,
                            Volume: 0,
                            Consume: true)
                    },
                    Products: new List<BlueprintProduct>
                    {
                        new(ProductId: 100, ProductName: "Test Product", Quantity: 1, Probability: 1.0)
                    })
            });

        var mockBlueprints = new Mock<IBlueprintService>(MockBehavior.Strict);
        mockBlueprints
            .Setup(x => x.GetDetailsAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        mockBlueprints
            .Setup(x => x.GetRawMaterialsAsync(
                It.IsAny<RawMaterialsRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RawMaterialsResponse(
                blueprintId,
                "Test Blueprint",
                ComponentMaterials: new List<MaterialBreakdown>(),
                RawMaterials: new List<MaterialBreakdown>
                {
                    new(TypeId: 300, TypeName: "Raw A", Quantity: 5, IsManufacturable: false)
                }));

        var now = DateTime.UtcNow;
        var mockPrices = new Mock<IMarketPriceService>(MockBehavior.Strict);
        mockPrices
            .Setup(x => x.GetCachedPricesAsync(It.IsAny<IEnumerable<int>>(), regionId))
            .ReturnsAsync((IEnumerable<int> typeIds, int _) =>
            {
                var dict = new Dictionary<int, MarketPrice>();
                foreach (var id in typeIds.Distinct())
                {
                    dict[id] = id switch
                    {
                        100 => new MarketPrice(id, regionId, 0m, 50m, 0, now, now.AddHours(1)),
                        200 => new MarketPrice(id, regionId, 0m, 10m, 0, now, now.AddHours(1)),
                        300 => new MarketPrice(id, regionId, 0m, 2m, 0, now, now.AddHours(1)),
                        _ => new MarketPrice(id, regionId, 0m, 0m, 0, now, now.AddHours(1))
                    };
                }

                return dict;
            });

        var mockIndustryCosts = new Mock<IIndustryCostService>(MockBehavior.Strict);
        var service = new ManufacturingService(mockBlueprints.Object, mockPrices.Object, mockIndustryCosts.Object);

        var request = new ManufacturingRequest(
            BlueprintId: blueprintId,
            MaterialEfficiency: 10,
            TimeEfficiency: 20,
            TotalUnits: 10,
            ProductionLines: 2,
            RegionId: regionId);

        var result = await service.CalculateAsync(request);

        result.Warnings.Should().BeEmpty();
        result.ComponentTotalCost.Should().Be(180m); // 2 qty * 10 runs * 0.9 => 18 units @ 10 ISK
        result.ProductValue.Should().Be(500m);
        result.Profit.Should().Be(320m);
        result.Iph.Should().BeApproximately(480m, 0.0001m); // 2400s = 2/3h; division can introduce repeating-decimal rounding

        mockBlueprints.VerifyAll();
        mockPrices.VerifyAll();
        mockIndustryCosts.VerifyAll();
    }

    [Fact]
    public async Task CalculateAsync_AppliesFacilityMaterialMultiplier_ToAdjustedQuantity()
    {
        var blueprintId = 2L;
        var regionId = 10000002;

        var details = new BlueprintDetails(
            BlueprintId: blueprintId,
            BlueprintName: "Test Blueprint 2",
            ItemGroup: "Test",
            ItemCategory: "Test",
            BaseProductionTimeSeconds: 1,
            Activities: new List<BlueprintActivity>
            {
                new(
                    ActivityId: 1,
                    ActivityName: "Manufacturing",
                    ProductId: 101,
                    ProductName: "Test Product 2",
                    ProductQuantity: 1,
                    Materials: new List<BlueprintMaterial>
                    {
                        new(
                            MaterialId: 201,
                            MaterialName: "Test Material 2",
                            MaterialGroup: "Test",
                            MaterialCategory: "Test",
                            Quantity: 2,
                            Volume: 0,
                            Consume: true)
                    },
                    Products: new List<BlueprintProduct>
                    {
                        new(ProductId: 101, ProductName: "Test Product 2", Quantity: 1, Probability: 1.0)
                    })
            });

        var mockBlueprints = new Mock<IBlueprintService>(MockBehavior.Strict);
        mockBlueprints
            .Setup(x => x.GetDetailsAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        mockBlueprints
            .Setup(x => x.GetRawMaterialsAsync(It.IsAny<RawMaterialsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RawMaterialsResponse(blueprintId, "Test Blueprint 2", new List<MaterialBreakdown>(), new List<MaterialBreakdown>()));

        var now = DateTime.UtcNow;
        var mockPrices = new Mock<IMarketPriceService>(MockBehavior.Strict);
        mockPrices
            .Setup(x => x.GetCachedPricesAsync(It.IsAny<IEnumerable<int>>(), regionId))
            .ReturnsAsync((IEnumerable<int> typeIds, int _) =>
            {
                var dict = new Dictionary<int, MarketPrice>();
                foreach (var id in typeIds.Distinct())
                {
                    dict[id] = id switch
                    {
                        101 => new MarketPrice(id, regionId, 0m, 50m, 0, now, now.AddHours(1)),
                        201 => new MarketPrice(id, regionId, 0m, 10m, 0, now, now.AddHours(1)),
                        _ => new MarketPrice(id, regionId, 0m, 0m, 0, now, now.AddHours(1))
                    };
                }

                return dict;
            });

        var mockIndustryCosts = new Mock<IIndustryCostService>(MockBehavior.Strict);
        var service = new ManufacturingService(mockBlueprints.Object, mockPrices.Object, mockIndustryCosts.Object);

        var request = new ManufacturingRequest(
            BlueprintId: blueprintId,
            MaterialEfficiency: 10,
            TotalUnits: 10,
            FacilityMaterialMultiplier: 1.1m,
            RegionId: regionId);

        var result = await service.CalculateAsync(request);

        result.ComponentMaterials.Should().ContainSingle();
        result.ComponentMaterials[0].Quantity.Should().Be(20);
        result.ComponentTotalCost.Should().Be(200m);

        mockBlueprints.VerifyAll();
        mockPrices.VerifyAll();
        mockIndustryCosts.VerifyAll();
    }

    [Fact]
    public async Task CalculateAsync_ComputesJobInstallationCost_FromSystemIndexAndAdjustedPrices_WhenOptedIn()
    {
        var blueprintId = 3L;
        var regionId = 10000002;

        var details = new BlueprintDetails(
            BlueprintId: blueprintId,
            BlueprintName: "Test Blueprint 3",
            ItemGroup: "Test",
            ItemCategory: "Test",
            BaseProductionTimeSeconds: 1,
            Activities: new List<BlueprintActivity>
            {
                new(
                    ActivityId: 1,
                    ActivityName: "Manufacturing",
                    ProductId: 102,
                    ProductName: "Test Product 3",
                    ProductQuantity: 1,
                    Materials: new List<BlueprintMaterial>
                    {
                        new(
                            MaterialId: 202,
                            MaterialName: "Mat A",
                            MaterialGroup: "Test",
                            MaterialCategory: "Test",
                            Quantity: 2,
                            Volume: 0,
                            Consume: true)
                    },
                    Products: new List<BlueprintProduct>
                    {
                        new(ProductId: 102, ProductName: "Test Product 3", Quantity: 1, Probability: 1.0)
                    })
            });

        var mockBlueprints = new Mock<IBlueprintService>(MockBehavior.Strict);
        mockBlueprints
            .Setup(x => x.GetDetailsAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        mockBlueprints
            .Setup(x => x.GetRawMaterialsAsync(It.IsAny<RawMaterialsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RawMaterialsResponse(blueprintId, "Test Blueprint 3", new List<MaterialBreakdown>(), new List<MaterialBreakdown>()));

        var now = DateTime.UtcNow;
        var mockPrices = new Mock<IMarketPriceService>(MockBehavior.Strict);
        mockPrices
            .Setup(x => x.GetCachedPricesAsync(It.IsAny<IEnumerable<int>>(), regionId))
            .ReturnsAsync((IEnumerable<int> typeIds, int _) =>
            {
                var dict = new Dictionary<int, MarketPrice>();
                foreach (var id in typeIds.Distinct())
                {
                    dict[id] = id switch
                    {
                        102 => new MarketPrice(id, regionId, 0m, 50m, 0, now, now.AddHours(1)),
                        202 => new MarketPrice(id, regionId, 0m, 10m, 0, now, now.AddHours(1)),
                        _ => new MarketPrice(id, regionId, 0m, 0m, 0, now, now.AddHours(1))
                    };
                }
                return dict;
            });

        var mockIndustryCosts = new Mock<IIndustryCostService>(MockBehavior.Strict);
        mockIndustryCosts
            .Setup(x => x.GetAdjustedPricesAsync(
                It.IsAny<IEnumerable<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, decimal>
            {
                // Adjusted price drives EIV used for installation cost, separate from market sell price
                [202] = 100m
            });

        var service = new ManufacturingService(mockBlueprints.Object, mockPrices.Object, mockIndustryCosts.Object);

        var request = new ManufacturingRequest(
            BlueprintId: blueprintId,
            MaterialEfficiency: 10,
            TotalUnits: 10,
            JobInstallationCost: 0m,
            SystemCostIndex: 0.10m,
            FacilityCostMultiplier: 1m,
            FacilityTaxRate: 0.05m,
            SccSurchargeRate: 0.02m,
            RegionId: regionId);

        var result = await service.CalculateAsync(request);

        // Component market cost: qty 2 * 10 runs * 0.9 => 18 units @ 10 ISK = 180
        // Product value: 10 * 50 ISK = 500
        // EIV from adjusted prices: 18 * 100 = 1800
        // Installation cost model: 1800 * (0.10*1 + 0.05 + 0.02) = 306
        result.Profit.Should().Be(14m);

        mockBlueprints.VerifyAll();
        mockPrices.VerifyAll();
        mockIndustryCosts.VerifyAll();
    }

    [Fact]
    public async Task CalculateAsync_ProductMarketMode_Sell_UsesBuyPrice_AndTaxOnly()
    {
        var blueprintId = 4L;
        var regionId = 10000002;

        var details = new BlueprintDetails(
            BlueprintId: blueprintId,
            BlueprintName: "Test Blueprint 4",
            ItemGroup: "Test",
            ItemCategory: "Test",
            BaseProductionTimeSeconds: 1,
            Activities: new List<BlueprintActivity>
            {
                new(
                    ActivityId: 1,
                    ActivityName: "Manufacturing",
                    ProductId: 103,
                    ProductName: "Test Product 4",
                    ProductQuantity: 1,
                    Materials: new List<BlueprintMaterial>
                    {
                        new(
                            MaterialId: 203,
                            MaterialName: "Test Material 4",
                            MaterialGroup: "Test",
                            MaterialCategory: "Test",
                            Quantity: 1,
                            Volume: 0,
                            Consume: true)
                    },
                    Products: new List<BlueprintProduct>
                    {
                        new(ProductId: 103, ProductName: "Test Product 4", Quantity: 1, Probability: 1.0)
                    })
            });

        var mockBlueprints = new Mock<IBlueprintService>(MockBehavior.Strict);
        mockBlueprints
            .Setup(x => x.GetDetailsAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        mockBlueprints
            .Setup(x => x.GetRawMaterialsAsync(It.IsAny<RawMaterialsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RawMaterialsResponse(blueprintId, "Test Blueprint 4", new List<MaterialBreakdown>(), new List<MaterialBreakdown>()));

        var now = DateTime.UtcNow;
        var mockPrices = new Mock<IMarketPriceService>(MockBehavior.Strict);
        mockPrices
            .Setup(x => x.GetCachedPricesAsync(It.IsAny<IEnumerable<int>>(), regionId))
            .ReturnsAsync((IEnumerable<int> typeIds, int _) =>
            {
                var dict = new Dictionary<int, MarketPrice>();
                foreach (var id in typeIds.Distinct())
                {
                    dict[id] = id switch
                    {
                        // Product has both buy and sell prices; Sell mode should use buyPrice (max buy)
                        103 => new MarketPrice(id, regionId, 40m, 50m, 0, now, now.AddHours(1)),
                        203 => new MarketPrice(id, regionId, 0m, 10m, 0, now, now.AddHours(1)),
                        _ => new MarketPrice(id, regionId, 0m, 0m, 0, now, now.AddHours(1))
                    };
                }

                return dict;
            });

        var mockIndustryCosts = new Mock<IIndustryCostService>(MockBehavior.Strict);
        var service = new ManufacturingService(mockBlueprints.Object, mockPrices.Object, mockIndustryCosts.Object);

        var request = new ManufacturingRequest(
            BlueprintId: blueprintId,
            MaterialEfficiency: 0,
            TotalUnits: 10,
            SalesTaxRate: 0.10m,
            BrokerFeeRate: 0.05m,
            ProductMarketMode: "Sell",
            RegionId: regionId);

        var result = await service.CalculateAsync(request);

        // Material cost: 1 * 10 @ 10 = 100
        // Product value (Sell mode): 10 * buyPrice(40) = 400
        // Sales tax applied: 400 * 0.10 = 40
        // Broker fee NOT applied in Sell mode
        result.ProductValue.Should().Be(400m);
        result.SalesTax.Should().Be(40m);
        result.BrokerFee.Should().Be(0m);
        result.Profit.Should().Be(260m);

        mockBlueprints.VerifyAll();
        mockPrices.VerifyAll();
        mockIndustryCosts.VerifyAll();
    }

    [Fact]
    public async Task CalculateAsync_MaterialMarketMode_BuyOrder_UsesBuyPrice_AndAddsBrokerFee()
    {
        var blueprintId = 5L;
        var regionId = 10000002;

        var details = new BlueprintDetails(
            BlueprintId: blueprintId,
            BlueprintName: "Test Blueprint 5",
            ItemGroup: "Test",
            ItemCategory: "Test",
            BaseProductionTimeSeconds: 1,
            Activities: new List<BlueprintActivity>
            {
                new(
                    ActivityId: 1,
                    ActivityName: "Manufacturing",
                    ProductId: 104,
                    ProductName: "Test Product 5",
                    ProductQuantity: 1,
                    Materials: new List<BlueprintMaterial>
                    {
                        new(
                            MaterialId: 204,
                            MaterialName: "Test Material 5",
                            MaterialGroup: "Test",
                            MaterialCategory: "Test",
                            Quantity: 1,
                            Volume: 0,
                            Consume: true)
                    },
                    Products: new List<BlueprintProduct>
                    {
                        new(ProductId: 104, ProductName: "Test Product 5", Quantity: 1, Probability: 1.0)
                    })
            });

        var mockBlueprints = new Mock<IBlueprintService>(MockBehavior.Strict);
        mockBlueprints
            .Setup(x => x.GetDetailsAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        mockBlueprints
            .Setup(x => x.GetRawMaterialsAsync(It.IsAny<RawMaterialsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RawMaterialsResponse(blueprintId, "Test Blueprint 5", new List<MaterialBreakdown>(), new List<MaterialBreakdown>()));

        var now = DateTime.UtcNow;
        var mockPrices = new Mock<IMarketPriceService>(MockBehavior.Strict);
        mockPrices
            .Setup(x => x.GetCachedPricesAsync(It.IsAny<IEnumerable<int>>(), regionId))
            .ReturnsAsync((IEnumerable<int> typeIds, int _) =>
            {
                var dict = new Dictionary<int, MarketPrice>();
                foreach (var id in typeIds.Distinct())
                {
                    dict[id] = id switch
                    {
                        104 => new MarketPrice(id, regionId, 0m, 50m, 0, now, now.AddHours(1)),
                        // Material has buy + sell; buy order uses buyPrice
                        204 => new MarketPrice(id, regionId, 8m, 10m, 0, now, now.AddHours(1)),
                        _ => new MarketPrice(id, regionId, 0m, 0m, 0, now, now.AddHours(1))
                    };
                }

                return dict;
            });

        var mockIndustryCosts = new Mock<IIndustryCostService>(MockBehavior.Strict);
        var service = new ManufacturingService(mockBlueprints.Object, mockPrices.Object, mockIndustryCosts.Object);

        var request = new ManufacturingRequest(
            BlueprintId: blueprintId,
            TotalUnits: 10,
            BrokerFeeRate: 0.05m,
            MaterialMarketMode: "BuyOrder",
            ProductMarketMode: "Buy",
            RegionId: regionId);

        var result = await service.CalculateAsync(request);

        // Material acquisition via buy order: buyPrice(8) * (1 + broker 0.05) = 8.4
        result.ComponentTotalCost.Should().Be(84m);
        result.ProductValue.Should().Be(500m);
        result.Profit.Should().Be(416m);

        mockBlueprints.VerifyAll();
        mockPrices.VerifyAll();
        mockIndustryCosts.VerifyAll();
    }

    [Fact]
    public async Task CalculateAsync_RawMaterials_RespectMaterialMarketMode_BuyOrder()
    {
        var blueprintId = 6L;
        var regionId = 10000002;

        var details = new BlueprintDetails(
            BlueprintId: blueprintId,
            BlueprintName: "Test Blueprint 6",
            ItemGroup: "Test",
            ItemCategory: "Test",
            BaseProductionTimeSeconds: 1,
            Activities: new List<BlueprintActivity>
            {
                new(
                    ActivityId: 1,
                    ActivityName: "Manufacturing",
                    ProductId: 105,
                    ProductName: "Test Product 6",
                    ProductQuantity: 1,
                    Materials: new List<BlueprintMaterial>
                    {
                        new(
                            MaterialId: 205,
                            MaterialName: "Component Mat",
                            MaterialGroup: "Test",
                            MaterialCategory: "Test",
                            Quantity: 1,
                            Volume: 0,
                            Consume: true)
                    },
                    Products: new List<BlueprintProduct>
                    {
                        new(ProductId: 105, ProductName: "Test Product 6", Quantity: 1, Probability: 1.0)
                    })
            });

        var mockBlueprints = new Mock<IBlueprintService>(MockBehavior.Strict);
        mockBlueprints
            .Setup(x => x.GetDetailsAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        // Raw breakdown includes a different material type to ensure the raw pricing path is exercised.
        mockBlueprints
            .Setup(x => x.GetRawMaterialsAsync(It.IsAny<RawMaterialsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RawMaterialsResponse(
                blueprintId,
                "Test Blueprint 6",
                ComponentMaterials: new List<MaterialBreakdown>(),
                RawMaterials: new List<MaterialBreakdown>
                {
                    new(TypeId: 999, TypeName: "Raw X", Quantity: 10, IsManufacturable: false)
                }));

        var now = DateTime.UtcNow;
        var mockPrices = new Mock<IMarketPriceService>(MockBehavior.Strict);
        mockPrices
            .Setup(x => x.GetCachedPricesAsync(It.IsAny<IEnumerable<int>>(), regionId))
            .ReturnsAsync((IEnumerable<int> typeIds, int _) =>
            {
                var dict = new Dictionary<int, MarketPrice>();
                foreach (var id in typeIds.Distinct())
                {
                    dict[id] = id switch
                    {
                        105 => new MarketPrice(id, regionId, 0m, 50m, 0, now, now.AddHours(1)),
                        205 => new MarketPrice(id, regionId, 8m, 10m, 0, now, now.AddHours(1)),
                        999 => new MarketPrice(id, regionId, 2m, 3m, 0, now, now.AddHours(1)),
                        _ => new MarketPrice(id, regionId, 0m, 0m, 0, now, now.AddHours(1))
                    };
                }

                return dict;
            });

        var mockIndustryCosts = new Mock<IIndustryCostService>(MockBehavior.Strict);
        var service = new ManufacturingService(mockBlueprints.Object, mockPrices.Object, mockIndustryCosts.Object);

        var request = new ManufacturingRequest(
            BlueprintId: blueprintId,
            TotalUnits: 10,
            BrokerFeeRate: 0.05m,
            MaterialMarketMode: "BuyOrder",
            ProductMarketMode: "Buy",
            RegionId: regionId);

        var result = await service.CalculateAsync(request);

        // Raw X buy order: buyPrice(2) * (1 + broker 0.05) = 2.1 => 10 * 2.1 = 21
        result.RawTotalCost.Should().Be(21m);

        mockBlueprints.VerifyAll();
        mockPrices.VerifyAll();
        mockIndustryCosts.VerifyAll();
    }

    [Fact]
    public async Task CalculateAsync_WhenProfitCostBasisIsBuildBuy_ComputesAutoBuildBuy_WithOptionalExcessSellback()
    {
        var regionId = 10000002;

        var blueprintId = 1000L;
        var productTypeId = 5000L;
        var componentTypeId = 6000L;
        var componentBlueprintId = 6001L;
        var rawTypeId = 7000L;

        var mainDetails = new BlueprintDetails(
            BlueprintId: blueprintId,
            BlueprintName: "Main BP",
            ItemGroup: "Test",
            ItemCategory: "Test",
            BaseProductionTimeSeconds: 3600,
            Activities: new List<BlueprintActivity>
            {
                new(
                    ActivityId: 1,
                    ActivityName: "Manufacturing",
                    ProductId: productTypeId,
                    ProductName: "Final Product",
                    ProductQuantity: 1,
                    Materials: new List<BlueprintMaterial>
                    {
                        new(
                            MaterialId: componentTypeId,
                            MaterialName: "Component",
                            MaterialGroup: "Test",
                            MaterialCategory: "Test",
                            Quantity: 1,
                            Volume: 0,
                            Consume: true)
                    },
                    Products: new List<BlueprintProduct>
                    {
                        new(ProductId: productTypeId, ProductName: "Final Product", Quantity: 1, Probability: 1.0)
                    })
            });

        var componentDetails = new BlueprintDetails(
            BlueprintId: componentBlueprintId,
            BlueprintName: "Component BP",
            ItemGroup: "Test",
            ItemCategory: "Test",
            BaseProductionTimeSeconds: 1,
            Activities: new List<BlueprintActivity>
            {
                new(
                    ActivityId: 1,
                    ActivityName: "Manufacturing",
                    ProductId: componentTypeId,
                    ProductName: "Component",
                    ProductQuantity: 2,
                    Materials: new List<BlueprintMaterial>
                    {
                        new(
                            MaterialId: rawTypeId,
                            MaterialName: "Raw",
                            MaterialGroup: "Test",
                            MaterialCategory: "Test",
                            Quantity: 10,
                            Volume: 0,
                            Consume: true)
                    },
                    Products: new List<BlueprintProduct>
                    {
                        new(ProductId: componentTypeId, ProductName: "Component", Quantity: 2, Probability: 1.0)
                    })
            });

        var mockBlueprints = new Mock<IBlueprintService>(MockBehavior.Strict);
        mockBlueprints
            .Setup(x => x.GetDetailsAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainDetails);
        mockBlueprints
            .Setup(x => x.GetDetailsAsync(componentBlueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(componentDetails);

        mockBlueprints
            .Setup(x => x.GetRawMaterialsAsync(It.IsAny<RawMaterialsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RawMaterialsResponse(blueprintId, "Main BP", new List<MaterialBreakdown>(), new List<MaterialBreakdown>()));

        mockBlueprints
            .Setup(x => x.FindManufacturingBlueprintIdByProductTypeIdAsync(componentTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(componentBlueprintId);
        mockBlueprints
            .Setup(x => x.FindManufacturingBlueprintIdByProductTypeIdAsync(rawTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((long?)null);

        var now = DateTime.UtcNow;
        var mockPrices = new Mock<IMarketPriceService>(MockBehavior.Strict);
        mockPrices
            .Setup(x => x.GetCachedPricesAsync(It.IsAny<IEnumerable<int>>(), regionId))
            .ReturnsAsync((IEnumerable<int> typeIds, int _) =>
            {
                var productIdInt = (int)productTypeId;
                var componentIdInt = (int)componentTypeId;
                var rawIdInt = (int)rawTypeId;

                var dict = new Dictionary<int, MarketPrice>();
                foreach (var id in typeIds.Distinct())
                {
                    dict[id] = id == productIdInt
                        ? new MarketPrice(id, regionId, 0m, 100m, 0, now, now.AddHours(1))
                        : id == componentIdInt
                            ? new MarketPrice(id, regionId, 0m, 6m, 0, now, now.AddHours(1))
                            : id == rawIdInt
                                ? new MarketPrice(id, regionId, 0m, 1m, 0, now, now.AddHours(1))
                                : new MarketPrice(id, regionId, 0m, 0m, 0, now, now.AddHours(1));
                }

                return dict;
            });

        var mockIndustryCosts = new Mock<IIndustryCostService>(MockBehavior.Strict);
        var service = new ManufacturingService(mockBlueprints.Object, mockPrices.Object, mockIndustryCosts.Object);

        var request = new ManufacturingRequest(
            BlueprintId: blueprintId,
            TotalUnits: 1,
            ProfitCostBasis: "BuildBuy",
            SellExcessItems: true,
            SalesTaxRate: 0.10m,
            BrokerFeeRate: 0.05m,
            RegionId: regionId);

        var result = await service.CalculateAsync(request);

        result.BuildBuyTotalCost.Should().NotBeNull();
        result.BuildBuyTotalCost!.Value.Should().BeApproximately(4.9m, 0.0001m); // build inputs (10) - net excess sellback (6 * 0.85)
        result.ExcessSellValueNet.Should().NotBeNull();
        result.ExcessSellValueNet!.Value.Should().BeApproximately(5.1m, 0.0001m);

        result.Profit.Should().BeApproximately(80.1m, 0.0001m);
        result.Iph.Should().BeApproximately(80.1m, 0.0001m);

        mockBlueprints.VerifyAll();
        mockPrices.VerifyAll();
        mockIndustryCosts.VerifyAll();
    }
}
