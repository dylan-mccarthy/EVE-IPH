namespace server.Models;

public sealed record SettingsResponse(Dictionary<string, string> Values);
