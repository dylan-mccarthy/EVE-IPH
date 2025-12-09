using System.ComponentModel.DataAnnotations;

namespace server.Models;

public sealed record BlueprintSearchRequest(
    [Required] string Query,
    string? Group = null,
    string? Category = null,
    int? Page = null,
    int? PageSize = null);
