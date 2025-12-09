namespace server.Models;

public sealed record SettingsRequest(Dictionary<string, string> Values);
