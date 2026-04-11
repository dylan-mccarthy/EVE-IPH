namespace EVE.IPH.Domain.Core.Identifiers;

/// <summary>Strongly-typed identifier for an EVE solar system (solarSystemID in the SDE).</summary>
public readonly record struct SystemId(int Value)
{
    public override string ToString() => Value.ToString();
}
