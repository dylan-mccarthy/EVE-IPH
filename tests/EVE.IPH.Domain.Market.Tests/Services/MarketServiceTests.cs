using EVE.IPH.Domain.Core.Identifiers;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Core.Results;
using EVE.IPH.Domain.Market.Services;
using NSubstitute;

namespace EVE.IPH.Domain.Market.Tests.Services;

public sealed class MarketServiceTests
{
    [Fact]
    public async Task GetPricesAsync_WhenAllEntriesAreFresh_UsesCacheOnly()
    {
        IMarketCacheRepository cacheRepository = Substitute.For<IMarketCacheRepository>();
        IMarketPriceSource priceSource = Substitute.For<IMarketPriceSource>();
        IMarketPriceSourceResolver resolver = Substitute.For<IMarketPriceSourceResolver>();
        IMarketPriceSourcePreferenceProvider preferenceProvider = Substitute.For<IMarketPriceSourcePreferenceProvider>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 4, 11, 12, 0, 0, TimeSpan.Zero));
        TypeId typeId = new(34);
        RegionId regionId = new(10000002);
        const MarketPriceSourceKind sourceKind = MarketPriceSourceKind.EveMarketer;

        resolver.Resolve(sourceKind).Returns(priceSource);

        cacheRepository.GetAsync(typeId, regionId.Value, (int)sourceKind, Arg.Any<CancellationToken>())
            .Returns(Maybe<MarketCacheRecord>.Some(new MarketCacheRecord(
                typeId, 0, 8.5, 8.5, 7.9, 7.9, 0, 8.5, 8.5, 9.2, 9.2, regionId.Value,
                timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-10), (int)sourceKind)));

        MarketService sut = new(cacheRepository, resolver, preferenceProvider, timeProvider);

        Result<IReadOnlyDictionary<TypeId, MarketPrice>> result = await sut.GetPricesAsync([typeId], regionId, sourceKind, TimeSpan.FromHours(1));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainKey(typeId);
        result.Value[typeId].MinSell.Should().Be(9.2);
        await priceSource.DidNotReceive().GetPricesAsync(Arg.Any<IEnumerable<TypeId>>(), Arg.Any<RegionId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPricesAsync_WhenEntryIsMissing_FetchesAndCachesSourceValue()
    {
        IMarketCacheRepository cacheRepository = Substitute.For<IMarketCacheRepository>();
        IMarketPriceSource priceSource = Substitute.For<IMarketPriceSource>();
        IMarketPriceSourceResolver resolver = Substitute.For<IMarketPriceSourceResolver>();
        IMarketPriceSourcePreferenceProvider preferenceProvider = Substitute.For<IMarketPriceSourcePreferenceProvider>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 4, 11, 12, 0, 0, TimeSpan.Zero));
        TypeId typeId = new(35);
        RegionId regionId = new(10000002);
        const MarketPriceSourceKind sourceKind = MarketPriceSourceKind.Fuzzworks;

        resolver.Resolve(sourceKind).Returns(priceSource);
        cacheRepository.GetAsync(typeId, regionId.Value, (int)sourceKind, Arg.Any<CancellationToken>()).Returns(Maybe<MarketCacheRecord>.None);
        priceSource.GetPricesAsync(Arg.Any<IEnumerable<TypeId>>(), regionId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Success(new Dictionary<TypeId, MarketPrice>
            {
                [typeId] = new(typeId, 5.5, 5.1, 5.3),
            }));
        cacheRepository.UpsertAsync(Arg.Any<MarketCacheRecord>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<MarketCacheRecord>.Success(call.Arg<MarketCacheRecord>()));

        MarketService sut = new(cacheRepository, resolver, preferenceProvider, timeProvider);

        Result<IReadOnlyDictionary<TypeId, MarketPrice>> result = await sut.GetPricesAsync([typeId], regionId, sourceKind, TimeSpan.FromHours(1));

        result.IsSuccess.Should().BeTrue();
        result.Value[typeId].Average.Should().Be(5.3);
        await cacheRepository.Received(1).UpsertAsync(
            Arg.Is<MarketCacheRecord>(record =>
                record.TypeId == typeId &&
                record.RegionOrSystem == regionId.Value &&
                record.PriceSource == (int)sourceKind &&
                record.SellMin == 5.5 &&
                record.BuyMax == 5.1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPricesAsync_WhenEntryIsExpired_RefreshesFromSource()
    {
        IMarketCacheRepository cacheRepository = Substitute.For<IMarketCacheRepository>();
        IMarketPriceSource priceSource = Substitute.For<IMarketPriceSource>();
        IMarketPriceSourceResolver resolver = Substitute.For<IMarketPriceSourceResolver>();
        IMarketPriceSourcePreferenceProvider preferenceProvider = Substitute.For<IMarketPriceSourcePreferenceProvider>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 4, 11, 12, 0, 0, TimeSpan.Zero));
        TypeId typeId = new(36);
        RegionId regionId = new(10000002);
        const MarketPriceSourceKind sourceKind = MarketPriceSourceKind.Tranquility;

        resolver.Resolve(sourceKind).Returns(priceSource);
        cacheRepository.GetAsync(typeId, regionId.Value, (int)sourceKind, Arg.Any<CancellationToken>())
            .Returns(Maybe<MarketCacheRecord>.Some(new MarketCacheRecord(
                typeId, 0, 1.0, 1.0, 1.0, 1.0, 0, 1.0, 1.0, 1.0, 1.0, regionId.Value,
                timeProvider.GetUtcNow().UtcDateTime.AddHours(-2), (int)sourceKind)));
        priceSource.GetPricesAsync(Arg.Any<IEnumerable<TypeId>>(), regionId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Success(new Dictionary<TypeId, MarketPrice>
            {
                [typeId] = new(typeId, 6.1, 5.7, 5.9),
            }));
        cacheRepository.UpsertAsync(Arg.Any<MarketCacheRecord>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<MarketCacheRecord>.Success(call.Arg<MarketCacheRecord>()));

        MarketService sut = new(cacheRepository, resolver, preferenceProvider, timeProvider);

        Result<IReadOnlyDictionary<TypeId, MarketPrice>> result = await sut.GetPricesAsync([typeId], regionId, sourceKind, TimeSpan.FromMinutes(30));

        result.IsSuccess.Should().BeTrue();
        result.Value[typeId].MinSell.Should().Be(6.1);
        await priceSource.Received(1).GetPricesAsync(Arg.Is<IEnumerable<TypeId>>(ids => ids.SequenceEqual(new[] { typeId })), regionId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPricesAsync_WhenSomeEntriesAreFresh_OnlyFetchesMissingOnes()
    {
        IMarketCacheRepository cacheRepository = Substitute.For<IMarketCacheRepository>();
        IMarketPriceSource priceSource = Substitute.For<IMarketPriceSource>();
        IMarketPriceSourceResolver resolver = Substitute.For<IMarketPriceSourceResolver>();
        IMarketPriceSourcePreferenceProvider preferenceProvider = Substitute.For<IMarketPriceSourcePreferenceProvider>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 4, 11, 12, 0, 0, TimeSpan.Zero));
        TypeId cachedTypeId = new(37);
        TypeId missingTypeId = new(38);
        RegionId regionId = new(10000002);
        const MarketPriceSourceKind sourceKind = MarketPriceSourceKind.Fuzzworks;

        resolver.Resolve(sourceKind).Returns(priceSource);
        cacheRepository.GetAsync(cachedTypeId, regionId.Value, (int)sourceKind, Arg.Any<CancellationToken>())
            .Returns(Maybe<MarketCacheRecord>.Some(new MarketCacheRecord(
                cachedTypeId, 0, 2.2, 2.2, 2.0, 2.0, 0, 2.2, 2.2, 2.5, 2.5, regionId.Value,
                timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-5), (int)sourceKind)));
        cacheRepository.GetAsync(missingTypeId, regionId.Value, (int)sourceKind, Arg.Any<CancellationToken>())
            .Returns(Maybe<MarketCacheRecord>.None);
        priceSource.GetPricesAsync(Arg.Any<IEnumerable<TypeId>>(), regionId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Success(new Dictionary<TypeId, MarketPrice>
            {
                [missingTypeId] = new(missingTypeId, 9.4, 9.1, 9.2),
            }));
        cacheRepository.UpsertAsync(Arg.Any<MarketCacheRecord>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<MarketCacheRecord>.Success(call.Arg<MarketCacheRecord>()));

        MarketService sut = new(cacheRepository, resolver, preferenceProvider, timeProvider);

        Result<IReadOnlyDictionary<TypeId, MarketPrice>> result = await sut.GetPricesAsync([cachedTypeId, missingTypeId], regionId, sourceKind, TimeSpan.FromHours(1));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[cachedTypeId].MinSell.Should().Be(2.5);
        result.Value[missingTypeId].MinSell.Should().Be(9.4);
        await priceSource.Received(1).GetPricesAsync(Arg.Is<IEnumerable<TypeId>>(ids => ids.SequenceEqual(new[] { missingTypeId })), regionId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPricesAsync_WhenSourceFails_ReturnsFailure()
    {
        IMarketCacheRepository cacheRepository = Substitute.For<IMarketCacheRepository>();
        IMarketPriceSource priceSource = Substitute.For<IMarketPriceSource>();
        IMarketPriceSourceResolver resolver = Substitute.For<IMarketPriceSourceResolver>();
        IMarketPriceSourcePreferenceProvider preferenceProvider = Substitute.For<IMarketPriceSourcePreferenceProvider>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 4, 11, 12, 0, 0, TimeSpan.Zero));
        TypeId typeId = new(39);
        RegionId regionId = new(10000002);
        const MarketPriceSourceKind sourceKind = MarketPriceSourceKind.Fuzzworks;

        resolver.Resolve(sourceKind).Returns(priceSource);
        cacheRepository.GetAsync(typeId, regionId.Value, (int)sourceKind, Arg.Any<CancellationToken>()).Returns(Maybe<MarketCacheRecord>.None);
        priceSource.GetPricesAsync(Arg.Any<IEnumerable<TypeId>>(), regionId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Failure("MARKET_UNAVAILABLE", "upstream failed"));

        MarketService sut = new(cacheRepository, resolver, preferenceProvider, timeProvider);

        Result<IReadOnlyDictionary<TypeId, MarketPrice>> result = await sut.GetPricesAsync([typeId], regionId, sourceKind, TimeSpan.FromHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("MARKET_UNAVAILABLE");
        await cacheRepository.DidNotReceive().UpsertAsync(Arg.Any<MarketCacheRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPricesAsync_UsesRequestedProviderResolver()
    {
        IMarketCacheRepository cacheRepository = Substitute.For<IMarketCacheRepository>();
        IMarketPriceSource priceSource = Substitute.For<IMarketPriceSource>();
        IMarketPriceSourceResolver resolver = Substitute.For<IMarketPriceSourceResolver>();
        IMarketPriceSourcePreferenceProvider preferenceProvider = Substitute.For<IMarketPriceSourcePreferenceProvider>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 4, 11, 12, 0, 0, TimeSpan.Zero));
        TypeId typeId = new(40);
        RegionId regionId = new(10000002);
        const MarketPriceSourceKind sourceKind = MarketPriceSourceKind.Tranquility;

        resolver.Resolve(sourceKind).Returns(priceSource);
        cacheRepository.GetAsync(typeId, regionId.Value, (int)sourceKind, Arg.Any<CancellationToken>()).Returns(Maybe<MarketCacheRecord>.None);
        priceSource.GetPricesAsync(Arg.Any<IEnumerable<TypeId>>(), regionId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Success(new Dictionary<TypeId, MarketPrice>
            {
                [typeId] = new(typeId, 11.1, 10.8, 10.9),
            }));
        cacheRepository.UpsertAsync(Arg.Any<MarketCacheRecord>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<MarketCacheRecord>.Success(call.Arg<MarketCacheRecord>()));

        MarketService sut = new(cacheRepository, resolver, preferenceProvider, timeProvider);

        Result<IReadOnlyDictionary<TypeId, MarketPrice>> result = await sut.GetPricesAsync([typeId], regionId, sourceKind, TimeSpan.FromHours(1));

        result.IsSuccess.Should().BeTrue();
        resolver.Received(1).Resolve(sourceKind);
    }

    [Fact]
    public async Task GetPricesAsync_WithoutExplicitSource_UsesConfiguredPreference()
    {
        IMarketCacheRepository cacheRepository = Substitute.For<IMarketCacheRepository>();
        IMarketPriceSource priceSource = Substitute.For<IMarketPriceSource>();
        IMarketPriceSourceResolver resolver = Substitute.For<IMarketPriceSourceResolver>();
        IMarketPriceSourcePreferenceProvider preferenceProvider = Substitute.For<IMarketPriceSourcePreferenceProvider>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 4, 11, 12, 0, 0, TimeSpan.Zero));
        TypeId typeId = new(41);
        RegionId regionId = new(10000002);
        const MarketPriceSourceKind sourceKind = MarketPriceSourceKind.Fuzzworks;

        preferenceProvider.GetSelectedSourceAsync(Arg.Any<CancellationToken>())
            .Returns(Result<MarketPriceSourceKind>.Success(sourceKind));
        resolver.Resolve(sourceKind).Returns(priceSource);
        cacheRepository.GetAsync(typeId, regionId.Value, (int)sourceKind, Arg.Any<CancellationToken>()).Returns(Maybe<MarketCacheRecord>.None);
        priceSource.GetPricesAsync(Arg.Any<IEnumerable<TypeId>>(), regionId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<TypeId, MarketPrice>>.Success(new Dictionary<TypeId, MarketPrice>
            {
                [typeId] = new(typeId, 12.4, 12.1, 12.2),
            }));
        cacheRepository.UpsertAsync(Arg.Any<MarketCacheRecord>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<MarketCacheRecord>.Success(call.Arg<MarketCacheRecord>()));

        MarketService sut = new(cacheRepository, resolver, preferenceProvider, timeProvider);

        Result<IReadOnlyDictionary<TypeId, MarketPrice>> result = await sut.GetPricesAsync([typeId], regionId, TimeSpan.FromHours(1));

        result.IsSuccess.Should().BeTrue();
        await preferenceProvider.Received(1).GetSelectedSourceAsync(Arg.Any<CancellationToken>());
        resolver.Received(1).Resolve(sourceKind);
    }

    [Fact]
    public async Task GetPricesAsync_WhenConfiguredPreferenceFails_ReturnsFailure()
    {
        IMarketCacheRepository cacheRepository = Substitute.For<IMarketCacheRepository>();
        IMarketPriceSourceResolver resolver = Substitute.For<IMarketPriceSourceResolver>();
        IMarketPriceSourcePreferenceProvider preferenceProvider = Substitute.For<IMarketPriceSourcePreferenceProvider>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 4, 11, 12, 0, 0, TimeSpan.Zero));
        TypeId typeId = new(42);
        RegionId regionId = new(10000002);

        preferenceProvider.GetSelectedSourceAsync(Arg.Any<CancellationToken>())
            .Returns(Result<MarketPriceSourceKind>.Failure("INVALID_PRICE_SOURCE", "Unsupported source value."));

        MarketService sut = new(cacheRepository, resolver, preferenceProvider, timeProvider);

        Result<IReadOnlyDictionary<TypeId, MarketPrice>> result = await sut.GetPricesAsync([typeId], regionId, TimeSpan.FromHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_PRICE_SOURCE");
        resolver.DidNotReceiveWithAnyArgs().Resolve(default);
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}