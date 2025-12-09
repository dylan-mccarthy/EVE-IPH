using server.Models;

namespace server.Services.Settings;

public sealed class SettingsService : ISettingsService
{
    private readonly Dictionary<string, string> _store = new();

    public Task<SettingsResponse> GetAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new SettingsResponse(new Dictionary<string, string>(_store)));
    }

    public Task<SettingsResponse> SaveAsync(SettingsRequest request, CancellationToken ct = default)
    {
        foreach (var kv in request.Values)
        {
            _store[kv.Key] = kv.Value;
        }
        return Task.FromResult(new SettingsResponse(new Dictionary<string, string>(_store)));
    }
}
