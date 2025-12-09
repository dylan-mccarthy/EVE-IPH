namespace server.Models;

public sealed record HealthResponse(string Status, DateTimeOffset CheckedAtUtc, string Environment);
