using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Infrastructure.ESI.Market;
using NSubstitute;

namespace EVE.IPH.Infrastructure.ESI.Tests.Market;

public sealed class MarketPriceSourceResolverTests
{
    [Fact]
    public void Resolve_WhenSourceIsRegistered_ReturnsMatchingProvider()
    {
        IMarketPriceSource tranquility = Substitute.For<IMarketPriceSource>();
        tranquility.SourceKind.Returns(MarketPriceSourceKind.Tranquility);
        IMarketPriceSource fuzzworks = Substitute.For<IMarketPriceSource>();
        fuzzworks.SourceKind.Returns(MarketPriceSourceKind.Fuzzworks);

        MarketPriceSourceResolver sut = new([tranquility, fuzzworks]);

        IMarketPriceSource result = sut.Resolve(MarketPriceSourceKind.Fuzzworks);

        result.Should().BeSameAs(fuzzworks);
    }
}