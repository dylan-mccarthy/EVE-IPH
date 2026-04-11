using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Tests.Results;

public sealed class ErrorTests
{
    [Fact]
    public void Error_ToString_IncludesCodeAndMessage()
    {
        Error error = new("NOT_FOUND", "Item not found");

        error.ToString().Should().Be("[NOT_FOUND] Item not found");
    }

    [Fact]
    public void Error_WithSameProperties_AreEqual()
    {
        Error a = new("CODE", "Message");
        Error b = new("CODE", "Message");

        a.Should().Be(b);
    }

    [Fact]
    public void Error_WithDifferentCodes_AreNotEqual()
    {
        Error a = new("CODE_A", "Message");
        Error b = new("CODE_B", "Message");

        a.Should().NotBe(b);
    }
}
