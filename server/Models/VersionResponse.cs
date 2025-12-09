namespace server.Models;

public sealed record VersionResponse(string Name, string Version, string Environment, DateTimeOffset StartedAtUtc);
