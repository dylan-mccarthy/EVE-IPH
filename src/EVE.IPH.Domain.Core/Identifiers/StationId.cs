namespace EVE.IPH.Domain.Core.Identifiers;

/// <summary>Strongly-typed identifier for an EVE station or Upwell structure.</summary>
public readonly record struct StationId(long Value)
{
    public override string ToString() => Value.ToString();
}
