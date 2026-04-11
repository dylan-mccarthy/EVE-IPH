namespace EVE.IPH.Domain.Core.Identifiers;

/// <summary>Strongly-typed identifier for an EVE blueprint type (blueprintTypeID in the SDE).</summary>
public readonly record struct BlueprintId(long Value)
{
    public override string ToString() => Value.ToString();
}
