namespace EVE.IPH.Domain.Reprocessing.Models;

public sealed record ReprocessingCalculationResult(
    long RefineBatches,
    double TotalYield,
    long RecoveredMaterialQuantity);