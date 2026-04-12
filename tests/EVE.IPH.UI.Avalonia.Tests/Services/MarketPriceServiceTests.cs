using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Market.Services;
using EVE.IPH.Infrastructure.Settings.Models;
using EVE.IPH.UI.Avalonia.Services;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.Services;

public sealed class MarketPriceServiceTests
{
    [Fact]
    public async Task QueryService_LoadsSelectedSourceAndDefaults()
    {
        IMarketPriceSourcePreferenceProvider preferenceProvider = Substitute.For<IMarketPriceSourcePreferenceProvider>();
        ISettingsStore settingsStore = Substitute.For<ISettingsStore>();
        preferenceProvider.GetSelectedSourceAsync(Arg.Any<CancellationToken>()).Returns(Result<MarketPriceSourceKind>.Success(MarketPriceSourceKind.Tranquility));
        settingsStore.ReadAsync<UpdatePriceTabSettingsModel>(Arg.Any<CancellationToken>()).Returns(Maybe<UpdatePriceTabSettingsModel>.None);

        MarketPriceQueryService service = new(preferenceProvider, settingsStore);

        MarketPriceScreenData result = await service.GetScreenDataAsync();

        result.SelectedSource.Should().Be(MarketPriceSourceKind.Tranquility);
        result.RegionId.Should().Be(10000002);
        result.TypeIdsText.Should().Contain("34");
    }

    [Fact]
    public async Task QueryService_WhenSavedMarketSettingsExist_LoadsThem()
    {
        IMarketPriceSourcePreferenceProvider preferenceProvider = Substitute.For<IMarketPriceSourcePreferenceProvider>();
        ISettingsStore settingsStore = Substitute.For<ISettingsStore>();
        preferenceProvider.GetSelectedSourceAsync(Arg.Any<CancellationToken>()).Returns(Result<MarketPriceSourceKind>.Success(MarketPriceSourceKind.EveMarketer));
        settingsStore.ReadAsync<UpdatePriceTabSettingsModel>(Arg.Any<CancellationToken>()).Returns(Maybe<UpdatePriceTabSettingsModel>.Some(new UpdatePriceTabSettingsModel
        {
            ModernMarketRegionId = 10000043,
            ModernMarketTypeIds = "34, 35",
        }));

        MarketPriceQueryService service = new(preferenceProvider, settingsStore);

        MarketPriceScreenData result = await service.GetScreenDataAsync();

        result.SelectedSource.Should().Be(MarketPriceSourceKind.EveMarketer);
        result.RegionId.Should().Be(10000043);
        result.TypeIdsText.Should().Be("34, 35");
    }

    [Fact]
    public async Task CommandService_LoadPricesAsync_MapsPriceRows()
    {
        IMarketService marketService = Substitute.For<IMarketService>();
        IItemRepository itemRepository = Substitute.For<IItemRepository>();
        ISettingsStore settingsStore = Substitute.For<ISettingsStore>();
        marketService.GetPricesAsync(Arg.Any<IEnumerable<TypeId>>(), new RegionId(10000002), MarketPriceSourceKind.Fuzzworks, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Success(new Dictionary<TypeId, MarketPrice>
            {
                [new TypeId(34)] = new(new TypeId(34), 5.25d, 5.10d, 5.20d),
            }));
        itemRepository.GetItemNameAsync(new TypeId(34), Arg.Any<CancellationToken>()).Returns(Maybe<string>.Some("Tritanium"));
        settingsStore.ReadAsync<UpdatePriceTabSettingsModel>(Arg.Any<CancellationToken>()).Returns(Maybe<UpdatePriceTabSettingsModel>.None);
        settingsStore.WriteAsync(Arg.Any<UpdatePriceTabSettingsModel>(), Arg.Any<CancellationToken>()).Returns(Result<bool>.Success(true));

        MarketPriceCommandService service = new(marketService, itemRepository, settingsStore);

        Result<MarketPriceResult> result = await service.LoadPricesAsync(new MarketPriceRequest(10000002, "34", MarketPriceSourceKind.Fuzzworks));

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.Should().ContainSingle();
        result.Value.Rows[0].ItemName.Should().Be("Tritanium");
        result.Value.Rows[0].MinSell.Should().Be(5.25d);
        await settingsStore.Received(1).WriteAsync(
            Arg.Is<UpdatePriceTabSettingsModel>(settings =>
                settings.ModernMarketRegionId == 10000002 &&
                settings.ModernMarketTypeIds == "34" &&
                settings.PriceDataSource == (int)MarketPriceSourceKind.Fuzzworks),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CommandService_BuildWatchlistFromSavedSelectionAsync_UsesSupportedSavedCategories()
    {
        IMarketService marketService = Substitute.For<IMarketService>();
        IItemRepository itemRepository = Substitute.For<IItemRepository>();
        ISettingsStore settingsStore = Substitute.For<ISettingsStore>();
        settingsStore.ReadAsync<UpdatePriceTabSettingsModel>(Arg.Any<CancellationToken>()).Returns(Maybe<UpdatePriceTabSettingsModel>.Some(new UpdatePriceTabSettingsModel
        {
            AllRawMats = false,
            Minerals = true,
            Gas = true,
            IceProducts = false,
            Planetary = true,
            RawMaterials = false,
            Salvage = false,
            AdvancedComponents = false,
            FuelBlocks = true,
        }));
        itemRepository.GetItemsByGroupNamesAsync(Arg.Is<IReadOnlyCollection<string>>(names => names.Count == 1 && names.Contains("Mineral")), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ItemRecord>>.Success([new ItemRecord(new TypeId(34), "Tritanium", 0, "Mineral", 0, 0, 1)]));
        itemRepository.GetItemsByGroupNamesAsync(Arg.Is<IReadOnlyCollection<string>>(names => names.Contains("Harvestable Cloud") && names.Contains("Compressed Gas") && names.Count == 2), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ItemRecord>>.Success([new ItemRecord(new TypeId(28694), "Fullerite-C28", 0, "Harvestable Cloud", 0, 0, 1)]));
        itemRepository.GetItemsByCategoryPrefixAsync("Planetary", Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ItemRecord>>.Success([new ItemRecord(new TypeId(2393), "Electrolytes", 0, "Planetary Commodities", 0, 0, 1)]));
        itemRepository.GetItemsByGroupNamesAsync(Arg.Is<IReadOnlyCollection<string>>(names => names.Count == 1 && names.Contains("Fuel Block")), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ItemRecord>>.Success([new ItemRecord(new TypeId(4246), "Hydrogen Fuel Block", 0, "Fuel Block", 0, 0, 1)]));
        settingsStore.WriteAsync(Arg.Any<UpdatePriceTabSettingsModel>(), Arg.Any<CancellationToken>()).Returns(Result<bool>.Success(true));

        MarketPriceCommandService service = new(marketService, itemRepository, settingsStore);

        Result<MarketPriceWatchlistResult> result = await service.BuildWatchlistFromSavedSelectionAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.TypeIdsText.Should().Be("2393, 28694, 4246, 34");
        result.Value.StatusText.Should().Contain("Minerals").And.Contain("Gas").And.Contain("Planetary").And.Contain("Fuel blocks");
        await settingsStore.Received(1).WriteAsync(
            Arg.Is<UpdatePriceTabSettingsModel>(settings => settings.ModernMarketTypeIds == "2393, 28694, 4246, 34"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CommandService_BuildWatchlistFromSavedSelectionAsync_WhenSettingsMissing_ReturnsFailure()
    {
        IMarketService marketService = Substitute.For<IMarketService>();
        IItemRepository itemRepository = Substitute.For<IItemRepository>();
        ISettingsStore settingsStore = Substitute.For<ISettingsStore>();
        settingsStore.ReadAsync<UpdatePriceTabSettingsModel>(Arg.Any<CancellationToken>()).Returns(Maybe<UpdatePriceTabSettingsModel>.None);

        MarketPriceCommandService service = new(marketService, itemRepository, settingsStore);

        Result<MarketPriceWatchlistResult> result = await service.BuildWatchlistFromSavedSelectionAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("MISSING_UPDATE_PRICE_SETTINGS");
    }

    [Fact]
    public async Task CommandService_LoadPricesAsync_WhenTypeIdsMissing_ReturnsFailure()
    {
        IMarketService marketService = Substitute.For<IMarketService>();
        IItemRepository itemRepository = Substitute.For<IItemRepository>();
        ISettingsStore settingsStore = Substitute.For<ISettingsStore>();

        MarketPriceCommandService service = new(marketService, itemRepository, settingsStore);

        Result<MarketPriceResult> result = await service.LoadPricesAsync(new MarketPriceRequest(10000002, " , ", MarketPriceSourceKind.Fuzzworks));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_TYPE_IDS");
    }
}