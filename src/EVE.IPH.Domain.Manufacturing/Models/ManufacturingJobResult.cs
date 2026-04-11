namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ManufacturingJobResult(
    long RequiredMaterialQuantity,
    double SingleRunDurationSeconds,
    double TotalJobDurationSeconds,
    long JobsPerBatch,
    long FullJobSessions);