namespace EVE.IPH.Domain.Core.Identifiers;

/// <summary>Strongly-typed identifier for an EVE region (regionID in the SDE).</summary>
public readonly record struct RegionId(int Value)
{
    public override string ToString() => Value.ToString();
}
