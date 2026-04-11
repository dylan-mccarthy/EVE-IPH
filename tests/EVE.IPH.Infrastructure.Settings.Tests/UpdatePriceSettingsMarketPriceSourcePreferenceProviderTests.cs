using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Infrastructure.Settings.Models;
using NSubstitute;

namespace EVE.IPH.Infrastructure.Settings.Tests;

public sealed class UpdatePriceSettingsMarketPriceSourcePreferenceProviderTests
{
    [Fact]
    public async Task GetSelectedSourceAsync_WhenSettingsAreMissing_ReturnsFuzzworksDefault()
    {
        ISettingsStore settingsStore = Substitute.For<ISettingsStore>();
        settingsStore.ReadAsync<UpdatePriceTabSettingsModel>(Arg.Any<CancellationToken>())
            .Returns(Maybe<UpdatePriceTabSettingsModel>.None);

        UpdatePriceSettingsMarketPriceSourcePreferenceProvider sut = new(settingsStore);

        Result<MarketPriceSourceKind> result = await sut.GetSelectedSourceAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(MarketPriceSourceKind.Fuzzworks);
    }

    [Theory]
    [InlineData(0, MarketPriceSourceKind.Tranquility)]
    [InlineData(1, MarketPriceSourceKind.EveMarketer)]
    [InlineData(2, MarketPriceSourceKind.Fuzzworks)]
    public async Task GetSelectedSourceAsync_WhenSettingsContainKnownValue_MapsToEnum(int rawValue, MarketPriceSourceKind expected)
    {
        ISettingsStore settingsStore = Substitute.For<ISettingsStore>();
        settingsStore.ReadAsync<UpdatePriceTabSettingsModel>(Arg.Any<CancellationToken>())
            .Returns(Maybe<UpdatePriceTabSettingsModel>.Some(new UpdatePriceTabSettingsModel { PriceDataSource = rawValue }));

        UpdatePriceSettingsMarketPriceSourcePreferenceProvider sut = new(settingsStore);

        Result<MarketPriceSourceKind> result = await sut.GetSelectedSourceAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expected);
    }

    [Fact]
    public async Task GetSelectedSourceAsync_WhenSettingsContainUnknownValue_ReturnsFailure()
    {
        ISettingsStore settingsStore = Substitute.For<ISettingsStore>();
        settingsStore.ReadAsync<UpdatePriceTabSettingsModel>(Arg.Any<CancellationToken>())
            .Returns(Maybe<UpdatePriceTabSettingsModel>.Some(new UpdatePriceTabSettingsModel { PriceDataSource = 99 }));

        UpdatePriceSettingsMarketPriceSourcePreferenceProvider sut = new(settingsStore);

        Result<MarketPriceSourceKind> result = await sut.GetSelectedSourceAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_PRICE_SOURCE");
    }
}