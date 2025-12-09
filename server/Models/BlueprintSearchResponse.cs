namespace server.Models;

public sealed record BlueprintSearchResponse(IReadOnlyList<BlueprintSummary> Items, int Total, int Page, int PageSize);

public sealed record BlueprintSummary(long Id, string Name, string Group, string Category);
