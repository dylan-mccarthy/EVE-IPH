namespace EVE.IPH.Domain.Core.Identifiers;

/// <summary>Strongly-typed identifier for an EVE alliance.</summary>
public readonly record struct AllianceId(long Value)
{
    public override string ToString() => Value.ToString();
}
