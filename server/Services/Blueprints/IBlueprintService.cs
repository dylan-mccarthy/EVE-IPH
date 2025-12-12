using server.Models;

namespace server.Services.Blueprints;

public interface IBlueprintService
{
    Task<BlueprintSearchResponse> SearchAsync(BlueprintSearchRequest request, CancellationToken ct = default);
    Task<BlueprintDetails?> GetDetailsAsync(long blueprintId, CancellationToken ct = default);
    Task<RawMaterialsResponse?> GetRawMaterialsAsync(RawMaterialsRequest request, CancellationToken ct = default);

    Task<long?> FindManufacturingBlueprintIdByProductTypeIdAsync(long productTypeId, CancellationToken ct = default);
}
