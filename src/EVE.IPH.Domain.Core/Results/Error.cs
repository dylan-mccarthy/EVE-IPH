namespace EVE.IPH.Domain.Core.Results;

/// <summary>Represents a domain error with a short code and a human-readable message.</summary>
public sealed record Error(string Code, string Message)
{
    public override string ToString() => $"[{Code}] {Message}";
}
