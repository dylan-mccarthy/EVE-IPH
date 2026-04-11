namespace EVE.IPH.Infrastructure.ESI.Models;

/// <summary>
/// Name resolution result returned by the ESI universe names endpoint.
/// </summary>
public sealed record EsiEntityName(long Id, string Category, string Name);