using server.Models;

namespace server.Services.Settings;

public interface ISettingsService
{
    Task<SettingsResponse> GetAsync(CancellationToken ct = default);
    Task<SettingsResponse> SaveAsync(SettingsRequest request, CancellationToken ct = default);
}
