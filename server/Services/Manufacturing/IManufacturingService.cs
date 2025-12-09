using server.Models;

namespace server.Services.Manufacturing;

public interface IManufacturingService
{
    Task<ManufacturingResponse> CalculateAsync(ManufacturingRequest request, CancellationToken ct = default);
}
