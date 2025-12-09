namespace server.Models;

public sealed record ManufacturingRequest(long BlueprintId, int Runs, string? System = null, string? Region = null);
