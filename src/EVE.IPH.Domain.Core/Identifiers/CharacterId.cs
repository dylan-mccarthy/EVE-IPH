namespace EVE.IPH.Domain.Core.Identifiers;

/// <summary>Strongly-typed identifier for an EVE character.</summary>
public readonly record struct CharacterId(long Value)
{
    public override string ToString() => Value.ToString();
}
