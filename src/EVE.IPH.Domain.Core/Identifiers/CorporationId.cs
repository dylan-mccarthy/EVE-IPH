namespace EVE.IPH.Domain.Core.Identifiers;

/// <summary>Strongly-typed identifier for an EVE corporation.</summary>
public readonly record struct CorporationId(long Value)
{
    public override string ToString() => Value.ToString();
}
