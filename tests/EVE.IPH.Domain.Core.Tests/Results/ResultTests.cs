using EVE.IPH.Domain.Core.Results;

namespace EVE.IPH.Domain.Core.Tests.Results;

public sealed class ResultTests
{
    // ── Success construction ──────────────────────────────────────────────

    [Fact]
    public void Success_IsSuccess_ReturnsTrue()
    {
        Result<int> result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Success_Value_ReturnsWrappedValue()
    {
        Result<string> result = Result<string>.Success("hello");

        result.Value.Should().Be("hello");
    }

    [Fact]
    public void Success_AccessingError_ThrowsInvalidOperationException()
    {
        Result<int> result = Result<int>.Success(1);

        Action act = () => _ = result.Error;

        act.Should().Throw<InvalidOperationException>();
    }

    // ── Failure construction ──────────────────────────────────────────────

    [Fact]
    public void Failure_IsFailure_ReturnsTrue()
    {
        Result<int> result = Result<int>.Failure(new Error("ERR", "fail"));

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Failure_Error_ReturnsWrappedError()
    {
        Error error = new("ERR_CODE", "Something went wrong");
        Result<int> result = Result<int>.Failure(error);

        result.Error.Should().Be(error);
    }

    [Fact]
    public void Failure_AccessingValue_ThrowsInvalidOperationException()
    {
        Result<int> result = Result<int>.Failure("ERR", "fail");

        Action act = () => _ = result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Failure_FromCodeAndMessage_ErrorHasCorrectProperties()
    {
        Result<int> result = Result<int>.Failure("NOT_FOUND", "Resource not found");

        result.Error.Code.Should().Be("NOT_FOUND");
        result.Error.Message.Should().Be("Resource not found");
    }

    // ── Map ───────────────────────────────────────────────────────────────

    [Fact]
    public void Map_OnSuccess_TransformsValue()
    {
        Result<int> result = Result<int>.Success(5);

        Result<string> mapped = result.Map(v => v.ToString());

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("5");
    }

    [Fact]
    public void Map_OnFailure_PropagatesError()
    {
        Error error = new("ERR", "fail");
        Result<int> result = Result<int>.Failure(error);

        Result<string> mapped = result.Map(v => v.ToString());

        mapped.IsFailure.Should().BeTrue();
        mapped.Error.Should().Be(error);
    }

    [Fact]
    public void Map_WithNullSelector_ThrowsArgumentNullException()
    {
        Result<int> result = Result<int>.Success(1);

        Action act = () => result.Map<string>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ── Bind ──────────────────────────────────────────────────────────────

    [Fact]
    public void Bind_OnSuccess_ChainsToNextResult()
    {
        Result<int> result = Result<int>.Success(10);

        Result<string> bound = result.Bind(v => Result<string>.Success($"value={v}"));

        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("value=10");
    }

    [Fact]
    public void Bind_WhenBinderReturnsFailure_PropagatesFailure()
    {
        Result<int> result = Result<int>.Success(10);
        Error innerError = new("INNER", "inner fail");

        Result<string> bound = result.Bind(_ => Result<string>.Failure(innerError));

        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be(innerError);
    }

    [Fact]
    public void Bind_OnFailure_PropagatesOriginalError()
    {
        Error error = new("ORIGINAL", "original fail");
        Result<int> result = Result<int>.Failure(error);

        Result<string> bound = result.Bind(_ => Result<string>.Success("never"));

        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be(error);
    }

    // ── Match ─────────────────────────────────────────────────────────────

    [Fact]
    public void Match_OnSuccess_CallsOnSuccess()
    {
        Result<int> result = Result<int>.Success(7);

        string output = result.Match(
            onSuccess: v => $"ok:{v}",
            onFailure: e => $"err:{e.Code}");

        output.Should().Be("ok:7");
    }

    [Fact]
    public void Match_OnFailure_CallsOnFailure()
    {
        Result<int> result = Result<int>.Failure("ERR", "bad");

        string output = result.Match(
            onSuccess: v => $"ok:{v}",
            onFailure: e => $"err:{e.Code}");

        output.Should().Be("err:ERR");
    }

    // ── Switch ────────────────────────────────────────────────────────────

    [Fact]
    public void Switch_OnSuccess_ExecutesOnSuccessAction()
    {
        Result<int> result = Result<int>.Success(3);
        int captured = 0;

        result.Switch(
            onSuccess: v => captured = v,
            onFailure: _ => captured = -1);

        captured.Should().Be(3);
    }

    [Fact]
    public void Switch_OnFailure_ExecutesOnFailureAction()
    {
        Result<int> result = Result<int>.Failure("E", "err");
        string captured = string.Empty;

        result.Switch(
            onSuccess: _ => captured = "success",
            onFailure: e => captured = e.Code);

        captured.Should().Be("E");
    }

    // ── ToString ──────────────────────────────────────────────────────────

    [Fact]
    public void ToString_OnSuccess_IncludesSuccessAndValue()
    {
        Result<int> result = Result<int>.Success(42);

        result.ToString().Should().Contain("Success").And.Contain("42");
    }

    [Fact]
    public void ToString_OnFailure_IncludesFailureAndCode()
    {
        Result<int> result = Result<int>.Failure("CODE", "msg");

        result.ToString().Should().Contain("Failure").And.Contain("CODE");
    }
}
