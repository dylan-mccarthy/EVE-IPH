namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record InventionPlanResult(
    double InventionChance,
    int SingleInventedBlueprintRuns,
    int RequiredBlueprintCopies,
    int NumberOfInventionJobs,
    int NumberOfInventionSessions,
    int TotalInventedRuns,
    double PerInventionRunCost);