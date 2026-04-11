namespace EVE.IPH.Domain.Reprocessing.Models;

public sealed record BeltFlipInput(
    IReadOnlyList<BeltFlipLineInput> Lines,
    double MiningVolumePerHourPerMiner,
    int MinerCount,
    bool CalculatePerMiner,
    bool UseCompressedSaleValues);