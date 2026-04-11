namespace EVE.IPH.Domain.Reprocessing.Models;

public sealed record BeltFlipResult(
    double RawSaleValue,
    double RefinedSaleValue,
    double MiningVolume,
    double DisplayVolume,
    double HoursToFlip,
    double HoursToFlipPerMiner,
    double RawSaleIskPerHour,
    double RefinedIskPerHour,
    double TotalReprocessingUsage,
    BeltFlipOutcome BetterOutcome);