namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record ComponentProductionScheduleInput(
    IReadOnlyList<double> ProductionTimesSeconds,
    int AvailableProductionLines);