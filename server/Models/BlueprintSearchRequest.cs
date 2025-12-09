namespace server.Models;

public sealed record BlueprintSearchRequest(
    string? Query = null,
    string? Group = null,
    string? Category = null,
    int? Page = null,
    int? PageSize = null);
