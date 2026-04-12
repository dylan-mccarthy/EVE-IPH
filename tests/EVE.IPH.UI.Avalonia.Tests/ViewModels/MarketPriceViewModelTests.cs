using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.UI.Avalonia.Services;
using EVE.IPH.UI.Avalonia.ViewModels;
using NSubstitute;

namespace EVE.IPH.UI.Avalonia.Tests.ViewModels;

public sealed class MarketPriceViewModelTests
{
    [Fact]
    public async Task LoadTask_LoadsDefaults()
    {
        IMarketPriceQueryService queryService = Substitute.For<IMarketPriceQueryService>();
        IMarketPriceCommandService commandService = Substitute.For<IMarketPriceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(CreateScreenData());

        MarketPriceViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        viewModel.RegionId.Should().Be(10000002);
        viewModel.TypeIdsText.Should().Contain("34");
        viewModel.SelectedSource.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadPricesAsync_WhenSuccessful_UpdatesRows()
    {
        IMarketPriceQueryService queryService = Substitute.For<IMarketPriceQueryService>();
        IMarketPriceCommandService commandService = Substitute.For<IMarketPriceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(CreateScreenData());
        commandService.LoadPricesAsync(Arg.Any<MarketPriceRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<MarketPriceResult>.Success(new MarketPriceResult(
                [new MarketPriceRow(34, "Tritanium", 5.25d, 5.10d, 5.20d)],
                "Loaded 1 market price snapshot for region 10000002 using Fuzzworks.")));

        MarketPriceViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        await viewModel.LoadPricesAsync();

        viewModel.Items.Should().ContainSingle();
        viewModel.StatusText.Should().Contain("Loaded 1 market price snapshot");
    }

    [Fact]
    public async Task LoadPricesAsync_WhenCommandFails_ExposesFailureStatus()
    {
        IMarketPriceQueryService queryService = Substitute.For<IMarketPriceQueryService>();
        IMarketPriceCommandService commandService = Substitute.For<IMarketPriceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(CreateScreenData());
        commandService.LoadPricesAsync(Arg.Any<MarketPriceRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<MarketPriceResult>.Failure("INVALID_TYPE_IDS", "No valid numeric item type IDs were found."));

        MarketPriceViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        await viewModel.LoadPricesAsync();

        viewModel.Items.Should().BeEmpty();
        viewModel.StatusText.Should().Contain("Unable to load market prices");
    }

    [Fact]
    public async Task BuildWatchlistFromSavedSelectionAsync_WhenSuccessful_UpdatesTypeIds()
    {
        IMarketPriceQueryService queryService = Substitute.For<IMarketPriceQueryService>();
        IMarketPriceCommandService commandService = Substitute.For<IMarketPriceCommandService>();
        queryService.GetScreenDataAsync(Arg.Any<CancellationToken>()).Returns(CreateScreenData());
        commandService.BuildWatchlistFromSavedSelectionAsync(Arg.Any<CancellationToken>())
            .Returns(Result<MarketPriceWatchlistResult>.Success(new MarketPriceWatchlistResult(
                "34, 35, 36",
                "Built a 3-item watchlist from saved update-price categories: Minerals, Gas.")));

        MarketPriceViewModel viewModel = new(queryService, commandService);
        await viewModel.LoadTask;

        await viewModel.BuildWatchlistFromSavedSelectionAsync();

        viewModel.TypeIdsText.Should().Be("34, 35, 36");
        viewModel.StatusText.Should().Contain("Built a 3-item watchlist");
    }

    private static MarketPriceScreenData CreateScreenData() => new(
        10000002,
        "34, 35, 36, 37",
        MarketPriceSourceKind.Fuzzworks,
        [
            new MarketPriceSourceOption(MarketPriceSourceKind.Tranquility, "Tranquility"),
            new MarketPriceSourceOption(MarketPriceSourceKind.EveMarketer, "EVE Marketer"),
            new MarketPriceSourceOption(MarketPriceSourceKind.Fuzzworks, "Fuzzworks"),
        ],
        "Enter item type IDs and a region ID to load live market prices through the modern market service.");
}