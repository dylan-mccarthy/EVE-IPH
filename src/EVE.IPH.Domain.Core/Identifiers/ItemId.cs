namespace EVE.IPH.Domain.Core.Identifiers;

/// <summary>Strongly-typed identifier for an EVE inventory item instance.</summary>
public readonly record struct ItemId(long Value)
{
    public override string ToString() => Value.ToString();
}
