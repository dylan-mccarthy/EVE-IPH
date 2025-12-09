using server.Models;

namespace server.Services.Manufacturing;

public sealed class ManufacturingService : IManufacturingService
{
    public Task<ManufacturingResponse> CalculateAsync(ManufacturingRequest request, CancellationToken ct = default)
    {
        // TODO: Implement manufacturing calculations
        var response = new ManufacturingResponse(request.BlueprintId, request.Runs, 0m, 0m, 0m);
        return Task.FromResult(response);
    }
}
