namespace EVE.IPH.Domain.Core.Results;

/// <summary>
/// Discriminated union representing either a successful value of type <typeparamref name="T"/>
/// or a domain <see cref="Error"/>. Use this as the return type for operations that can fail
/// for expected, non-exceptional reasons.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    /// <summary>Returns <c>true</c> when the result represents a successful outcome.</summary>
    public bool IsSuccess { get; }

    /// <summary>Returns <c>true</c> when the result represents a failure.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The success value. Only valid when <see cref="IsSuccess"/> is <c>true</c>.</summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed on a failure result.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access Value on a failed Result. Error: {_error}");

    /// <summary>The error. Only valid when <see cref="IsFailure"/> is <c>true</c>.</summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed on a success result.</exception>
    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    /// <summary>Creates a successful result wrapping <paramref name="value"/>.</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>Creates a failure result wrapping <paramref name="error"/>.</summary>
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>Creates a failure result from a code and message.</summary>
    public static Result<T> Failure(string code, string message) => new(new Error(code, message));

    /// <summary>
    /// Projects the success value using <paramref name="selector"/>.
    /// Returns a failure result unchanged.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return IsSuccess ? Result<TOut>.Success(selector(_value!)) : Result<TOut>.Failure(_error!);
    }

    /// <summary>
    /// Chains a result-returning function onto the success path.
    /// Returns a failure result unchanged.
    /// </summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return IsSuccess ? binder(_value!) : Result<TOut>.Failure(_error!);
    }

    /// <summary>
    /// Pattern-matches the result, executing <paramref name="onSuccess"/> or <paramref name="onFailure"/>
    /// and returning the produced value.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(_value!) : onFailure(_error!);
    }

    /// <summary>
    /// Executes <paramref name="onSuccess"/> or <paramref name="onFailure"/> for side effects.
    /// </summary>
    public void Switch(Action<T> onSuccess, Action<Error> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        if (IsSuccess)
            onSuccess(_value!);
        else
            onFailure(_error!);
    }

    public override string ToString() =>
        IsSuccess ? $"Success({_value})" : $"Failure({_error})";
}
