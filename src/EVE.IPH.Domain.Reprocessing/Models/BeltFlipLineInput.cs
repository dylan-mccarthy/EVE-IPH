namespace EVE.IPH.Domain.Reprocessing.Models;

public sealed record BeltFlipLineInput(
    string ItemName,
    long Quantity,
    double MiningVolumePerUnit,
    double RawUnitSaleValue,
    double RefinedSaleValue,
    double ReprocessingUsage = 0d,
    int CompressionBatchSize = 100,
    double? CompressedBatchSaleValue = null,
    double? CompressedBatchVolume = null);