using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Tests.Results;

public sealed class MaybeTests
{
    // ── Construction ──────────────────────────────────────────────────────

    [Fact]
    public void Some_HasValue_ReturnsTrue()
    {
        Maybe<int> maybe = Maybe<int>.Some(42);

        maybe.HasValue.Should().BeTrue();
        maybe.HasNoValue.Should().BeFalse();
    }

    [Fact]
    public void Some_Value_ReturnsWrappedValue()
    {
        Maybe<string> maybe = Maybe<string>.Some("hello");

        maybe.Value.Should().Be("hello");
    }

    [Fact]
    public void None_HasValue_ReturnsFalse()
    {
        Maybe<int> maybe = Maybe<int>.None;

        maybe.HasValue.Should().BeFalse();
        maybe.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void None_AccessingValue_ThrowsInvalidOperationException()
    {
        Maybe<int> maybe = Maybe<int>.None;

        Action act = () => _ = maybe.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    // ── Map ───────────────────────────────────────────────────────────────

    [Fact]
    public void Map_OnSome_TransformsValue()
    {
        Maybe<int> maybe = Maybe<int>.Some(5);

        Maybe<string> mapped = maybe.Map(v => v.ToString());

        mapped.HasValue.Should().BeTrue();
        mapped.Value.Should().Be("5");
    }

    [Fact]
    public void Map_OnNone_ReturnsNone()
    {
        Maybe<int> maybe = Maybe<int>.None;

        Maybe<string> mapped = maybe.Map(v => v.ToString());

        mapped.HasValue.Should().BeFalse();
    }

    [Fact]
    public void Map_WithNullSelector_ThrowsArgumentNullException()
    {
        Maybe<int> maybe = Maybe<int>.Some(1);

        Action act = () => maybe.Map<string>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ── Bind ──────────────────────────────────────────────────────────────

    [Fact]
    public void Bind_OnSome_ChainsToNextMaybe()
    {
        Maybe<int> maybe = Maybe<int>.Some(10);

        Maybe<string> bound = maybe.Bind(v => Maybe<string>.Some($"value={v}"));

        bound.HasValue.Should().BeTrue();
        bound.Value.Should().Be("value=10");
    }

    [Fact]
    public void Bind_OnSome_WhenBinderReturnsNone_ReturnsNone()
    {
        Maybe<int> maybe = Maybe<int>.Some(10);

        Maybe<string> bound = maybe.Bind(_ => Maybe<string>.None);

        bound.HasValue.Should().BeFalse();
    }

    [Fact]
    public void Bind_OnNone_ReturnsNone()
    {
        Maybe<int> maybe = Maybe<int>.None;

        Maybe<string> bound = maybe.Bind(v => Maybe<string>.Some(v.ToString()));

        bound.HasValue.Should().BeFalse();
    }

    // ── GetValueOrDefault ─────────────────────────────────────────────────

    [Fact]
    public void GetValueOrDefault_OnSome_ReturnsValue()
    {
        Maybe<int> maybe = Maybe<int>.Some(7);

        maybe.GetValueOrDefault(0).Should().Be(7);
    }

    [Fact]
    public void GetValueOrDefault_OnNone_ReturnsDefault()
    {
        Maybe<int> maybe = Maybe<int>.None;

        maybe.GetValueOrDefault(99).Should().Be(99);
    }

    // ── Match ─────────────────────────────────────────────────────────────

    [Fact]
    public void Match_OnSome_CallsOnSome()
    {
        Maybe<int> maybe = Maybe<int>.Some(3);

        string result = maybe.Match(
            onSome: v => $"some:{v}",
            onNone: () => "none");

        result.Should().Be("some:3");
    }

    [Fact]
    public void Match_OnNone_CallsOnNone()
    {
        Maybe<int> maybe = Maybe<int>.None;

        string result = maybe.Match(
            onSome: v => $"some:{v}",
            onNone: () => "none");

        result.Should().Be("none");
    }

    // ── None singleton ────────────────────────────────────────────────────

    [Fact]
    public void None_IsSingleton()
    {
        Maybe<int> a = Maybe<int>.None;
        Maybe<int> b = Maybe<int>.None;

        ReferenceEquals(a, b).Should().BeTrue();
    }

    // ── ToString ──────────────────────────────────────────────────────────

    [Fact]
    public void ToString_OnSome_IncludesSomeAndValue()
    {
        Maybe<int> maybe = Maybe<int>.Some(42);

        maybe.ToString().Should().Contain("Some").And.Contain("42");
    }

    [Fact]
    public void ToString_OnNone_ReturnsNone()
    {
        Maybe<int> maybe = Maybe<int>.None;

        maybe.ToString().Should().Be("None");
    }
}
