namespace EVE.IPH.Domain.Core.Results;

/// <summary>
/// Option type that represents either a present value of type <typeparamref name="T"/>
/// or the absence of a value. Use this instead of <c>null</c> for domain return types
/// where the absence of a value is a normal, expected outcome.
/// </summary>
/// <typeparam name="T">The type of the contained value.</typeparam>
public sealed class Maybe<T>
{
    private readonly T? _value;

    private Maybe(T value)
    {
        _value = value;
        HasValue = true;
    }

    private Maybe()
    {
        _value = default;
        HasValue = false;
    }

    /// <summary>Returns <c>true</c> when a value is present.</summary>
    public bool HasValue { get; }

    /// <summary>Returns <c>true</c> when no value is present.</summary>
    public bool HasNoValue => !HasValue;

    /// <summary>The contained value. Only valid when <see cref="HasValue"/> is <c>true</c>.</summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed on an empty Maybe.</exception>
    public T Value => HasValue
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on an empty Maybe.");

    /// <summary>Creates a <see cref="Maybe{T}"/> with a present value.</summary>
    public static Maybe<T> Some(T value) => new(value);

    /// <summary>Creates an empty <see cref="Maybe{T}"/>.</summary>
    public static Maybe<T> None { get; } = new();

    /// <summary>
    /// Projects the contained value using <paramref name="selector"/>.
    /// Returns <see cref="Maybe{TOut}.None"/> when no value is present.
    /// </summary>
    public Maybe<TOut> Map<TOut>(Func<T, TOut> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return HasValue ? Maybe<TOut>.Some(selector(_value!)) : Maybe<TOut>.None;
    }

    /// <summary>
    /// Chains a Maybe-returning function. Returns <see cref="None"/> when no value is present.
    /// </summary>
    public Maybe<TOut> Bind<TOut>(Func<T, Maybe<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return HasValue ? binder(_value!) : Maybe<TOut>.None;
    }

    /// <summary>Returns the contained value, or <paramref name="defaultValue"/> when empty.</summary>
    public T GetValueOrDefault(T defaultValue) => HasValue ? _value! : defaultValue;

    /// <summary>
    /// Pattern-matches the Maybe, executing <paramref name="onSome"/> or <paramref name="onNone"/>
    /// and returning the produced value.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> onSome, Func<TOut> onNone)
    {
        ArgumentNullException.ThrowIfNull(onSome);
        ArgumentNullException.ThrowIfNull(onNone);
        return HasValue ? onSome(_value!) : onNone();
    }

    public override string ToString() => HasValue ? $"Some({_value})" : "None";
}
