using FluentAssertions;
using server.Services.Manufacturing;
using Xunit;

namespace server.Tests.Services.Manufacturing;

public sealed class MaterialMathTests
{
    [Theory]
    [InlineData(0, 10, 10, 0)]
    [InlineData(1, 0, 0, 0)]
    [InlineData(1, 10, 1, 1)]
    [InlineData(1, 10, 10, 10)]
    [InlineData(2, 10, 10, 18)]
    [InlineData(2, 10, 1, 2)]
    [InlineData(5, 7, 3, 14)]
    [InlineData(1, 60, 2, 2)]
    public void CalculateAdjustedQuantity_matchesLegacyBehavior(int baseQty, int me, int runs, int expected)
    {
        var actual = MaterialMath.CalculateAdjustedQuantity(baseQty, me, runs);
        actual.Should().Be(expected);
    }
}
