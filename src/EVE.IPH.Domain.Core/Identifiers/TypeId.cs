namespace EVE.IPH.Domain.Core.Identifiers;

/// <summary>Strongly-typed identifier for an EVE item type (typeID in the SDE).</summary>
public readonly record struct TypeId(long Value)
{
    public override string ToString() => Value.ToString();
}
