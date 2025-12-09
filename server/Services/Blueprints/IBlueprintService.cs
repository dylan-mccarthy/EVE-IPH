using server.Models;

namespace server.Services.Blueprints;

public interface IBlueprintService
{
    Task<BlueprintSearchResponse> SearchAsync(BlueprintSearchRequest request, CancellationToken ct = default);
}
