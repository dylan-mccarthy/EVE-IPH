namespace EVE.IPH.Infrastructure.ESI;

/// <summary>
/// One standing row returned by the character standings endpoint.
/// </summary>
public sealed record EsiStanding(
    long FromId,
    string FromType,
    double Standing);